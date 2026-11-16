import { useCallback, useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import type { RootState } from '../redux/store';
import { blobToBase64, getTextFromVoiceInput } from '../webapi/voiceInputSvc';
import appHelper from '../helper/appHelper';
import { readDictAppSetupBool } from '../helper/adminPermissionHelper';

const MAX_RECORD_MS = 60_000;

/** Which STT engine the mic will use. */
export type VoiceEngine = 'whisper' | 'webspeech' | 'none';

/**
 * Server Whisper/Gemini path is enabled when tenant voice flag is on.
 * Prefer IsAiVoiceInputEnabled; fall back to IsAiConfigured + AIConfigEnableVoiceInput.
 */
export function resolveServerVoiceEnabled(userContext: any): boolean {
  if (!userContext) return false;
  const explicit = userContext.IsAiVoiceInputEnabled ?? userContext.isAiVoiceInputEnabled;
  if (typeof explicit === 'boolean') return explicit;

  const aiOn = !!(userContext.IsAiConfigured ?? userContext.isAiConfigured);
  if (!aiOn) return false;
  const dict = userContext.DictAppSetup ?? userContext.dictAppSetup;
  return readDictAppSetupBool(dict, 'AIConfigEnableVoiceInput', true);
}

function mediaRecorderSupported(): boolean {
  try {
    return typeof window !== 'undefined' && typeof (window as any).MediaRecorder === 'function';
  } catch {
    return false;
  }
}

function webSpeechSupported(): boolean {
  try {
    return (
      typeof window !== 'undefined'
      && (!!(window as any).SpeechRecognition || !!(window as any).webkitSpeechRecognition)
    );
  } catch {
    return false;
  }
}

function pickRecorderMimeType(): string | undefined {
  if (typeof MediaRecorder === 'undefined') return undefined;
  const candidates = [
    'audio/webm;codecs=opus',
    'audio/webm',
    'audio/ogg;codecs=opus',
    'audio/mp4',
  ];
  return candidates.find(t => {
    try {
      return MediaRecorder.isTypeSupported(t);
    } catch {
      return false;
    }
  });
}

function resolveEngine(serverVoiceEnabled: boolean): VoiceEngine {
  if (serverVoiceEnabled && mediaRecorderSupported()) return 'whisper';
  if (webSpeechSupported()) return 'webspeech';
  if (mediaRecorderSupported()) return 'whisper'; // last resort: try server even if flag unknown
  return 'none';
}

export type UseVoiceInputOptions = {
  tenantEnabled?: boolean;
};

/**
 * Dual-path voice input:
 * - Prefer MediaRecorder → Whisper/Gemini when tenant server voice is enabled
 * - Fallback: browser Web Speech API (SpeechRecognition)
 */
export function useVoiceInput(
  onTranscript: (text: string) => void,
  options?: UseVoiceInputOptions
) {
  const serverFromSession = useSelector((s: RootState) =>
    resolveServerVoiceEnabled(s.userSession?.userContext)
  );
  const serverVoiceEnabled = options?.tenantEnabled ?? serverFromSession;
  const engine = resolveEngine(serverVoiceEnabled);

  const [isRecording, setIsRecording] = useState(false);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const chunksRef = useRef<BlobPart[]>([]);
  const maxTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const recognitionRef = useRef<any>(null);
  const onTranscriptRef = useRef(onTranscript);
  onTranscriptRef.current = onTranscript;

  const isListening = isRecording || isTranscribing;

  const cleanupWhisper = useCallback(() => {
    if (maxTimerRef.current) {
      clearTimeout(maxTimerRef.current);
      maxTimerRef.current = null;
    }
    streamRef.current?.getTracks().forEach(t => t.stop());
    streamRef.current = null;
    mediaRecorderRef.current = null;
    chunksRef.current = [];
  }, []);

  const cleanupWebSpeech = useCallback(() => {
    try {
      recognitionRef.current?.stop?.();
    } catch { /* ignore */ }
    recognitionRef.current = null;
  }, []);

  useEffect(() => () => {
    cleanupWhisper();
    cleanupWebSpeech();
  }, [cleanupWhisper, cleanupWebSpeech]);

  // ── Whisper path ──────────────────────────────────────────────────────────

  const finishAndTranscribe = useCallback(async (blob: Blob, mimeType: string) => {
    setIsRecording(false);
    setIsTranscribing(true);
    setError(null);
    try {
      if (!blob.size) throw new Error('No audio was recorded. Check your microphone.');
      const base64 = await blobToBase64(blob);
      const sentence = await getTextFromVoiceInput(base64, mimeType);
      onTranscriptRef.current(sentence);
    } catch (e: any) {
      const msg = e?.message || 'Voice transcription failed.';
      setError(msg);
      appHelper.debugLog('useVoiceInput/whisper', msg);
    } finally {
      setIsTranscribing(false);
      cleanupWhisper();
    }
  }, [cleanupWhisper]);

  const stopWhisper = useCallback(() => {
    const rec = mediaRecorderRef.current;
    if (!rec || rec.state === 'inactive') {
      setIsRecording(false);
      cleanupWhisper();
      return;
    }
    try {
      rec.stop();
    } catch {
      setIsRecording(false);
      cleanupWhisper();
    }
  }, [cleanupWhisper]);

  const startWhisper = useCallback(async () => {
    if (isListening) return;
    if (!navigator.mediaDevices?.getUserMedia) {
      setError('Microphone API unavailable. Use https:// or localhost.');
      return;
    }
    setError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;
      const mimeType = pickRecorderMimeType();
      const rec = mimeType
        ? new MediaRecorder(stream, { mimeType })
        : new MediaRecorder(stream);
      chunksRef.current = [];
      mediaRecorderRef.current = rec;

      rec.ondataavailable = (ev) => {
        if (ev.data && ev.data.size > 0) chunksRef.current.push(ev.data);
      };
      rec.onerror = () => {
        setError('Microphone recording failed.');
        setIsRecording(false);
        cleanupWhisper();
      };
      rec.onstop = () => {
        const type = rec.mimeType || mimeType || 'audio/webm';
        const blob = new Blob(chunksRef.current, { type });
        void finishAndTranscribe(blob, type.split(';')[0] || 'audio/webm');
      };

      rec.start(250);
      setIsRecording(true);
      maxTimerRef.current = setTimeout(() => stopWhisper(), MAX_RECORD_MS);
    } catch (e: any) {
      const msg =
        e?.name === 'NotAllowedError'
          ? 'Microphone permission denied.'
          : e?.message || 'Could not access the microphone.';
      setError(msg);
      cleanupWhisper();
    }
  }, [isListening, cleanupWhisper, finishAndTranscribe, stopWhisper]);

  // ── Web Speech fallback ───────────────────────────────────────────────────

  const stopWebSpeech = useCallback(() => {
    try {
      recognitionRef.current?.stop?.();
    } catch { /* ignore */ }
  }, []);

  const startWebSpeech = useCallback(() => {
    if (isListening) return;
    setError(null);
    const SR: any =
      (window as any).SpeechRecognition ?? (window as any).webkitSpeechRecognition;
    if (!SR) {
      setError('Browser speech recognition is not supported.');
      return;
    }
    const rec = new SR();
    rec.lang = (typeof navigator !== 'undefined' && navigator.language) || 'en-US';
    rec.interimResults = false;
    rec.maxAlternatives = 1;
    rec.continuous = false;

    rec.onresult = (e: any) => {
      const transcript: string = e.results?.[0]?.[0]?.transcript ?? '';
      if (transcript) onTranscriptRef.current(transcript);
    };
    rec.onerror = (e: any) => {
      const code = e?.error || '';
      if (code === 'aborted' || code === 'no-speech') {
        setIsRecording(false);
        return;
      }
      setError(code === 'not-allowed' ? 'Microphone permission denied.' : `Speech recognition error: ${code || 'unknown'}`);
      setIsRecording(false);
    };
    rec.onend = () => {
      setIsRecording(false);
      recognitionRef.current = null;
    };

    recognitionRef.current = rec;
    try {
      rec.start();
      setIsRecording(true);
    } catch (e: any) {
      setError(e?.message || 'Could not start speech recognition.');
      setIsRecording(false);
      recognitionRef.current = null;
    }
  }, [isListening]);

  const toggle = useCallback(() => {
    if (isTranscribing) return;
    if (engine === 'none') {
      setError('Voice input is not available in this browser.');
      return;
    }
    if (isRecording) {
      if (engine === 'whisper') stopWhisper();
      else stopWebSpeech();
      return;
    }
    if (engine === 'whisper') void startWhisper();
    else startWebSpeech();
  }, [isTranscribing, engine, isRecording, stopWhisper, stopWebSpeech, startWhisper, startWebSpeech]);

  return {
    isListening,
    isRecording,
    isTranscribing,
    /** Mic is shown whenever either engine can run. */
    supported: engine !== 'none',
    engine,
    serverVoiceEnabled,
    error,
    clearError: () => setError(null),
    toggle,
  };
}
