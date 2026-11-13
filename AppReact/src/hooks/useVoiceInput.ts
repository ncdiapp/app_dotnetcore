import { useCallback, useEffect, useRef, useState } from 'react';
import { blobToBase64, getTextFromVoiceInput } from '../webapi/voiceInputSvc';
import appHelper from '../helper/appHelper';

const MAX_RECORD_MS = 60_000;

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

/** True when MediaRecorder constructor exists (SSR-safe). */
export function mediaRecorderSupported(): boolean {
  try {
    return typeof window !== 'undefined' && typeof (window as any).MediaRecorder === 'function';
  } catch {
    return false;
  }
}

export type UseVoiceInputOptions = {
  /** Kept for API compatibility; visibility no longer depends on tenant flags. */
  tenantEnabled?: boolean;
};

/**
 * MediaRecorder → Whisper/Gemini STT for agent composers.
 * Always reports supported=true when MediaRecorder exists so MicButton can render.
 */
export function useVoiceInput(
  onTranscript: (text: string) => void,
  _options?: UseVoiceInputOptions
) {
  const [isRecording, setIsRecording] = useState(false);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const chunksRef = useRef<BlobPart[]>([]);
  const maxTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const onTranscriptRef = useRef(onTranscript);
  onTranscriptRef.current = onTranscript;

  const supported = mediaRecorderSupported();
  const isListening = isRecording || isTranscribing;

  const cleanupStream = useCallback(() => {
    if (maxTimerRef.current) {
      clearTimeout(maxTimerRef.current);
      maxTimerRef.current = null;
    }
    streamRef.current?.getTracks().forEach(t => t.stop());
    streamRef.current = null;
    mediaRecorderRef.current = null;
    chunksRef.current = [];
  }, []);

  useEffect(() => () => cleanupStream(), [cleanupStream]);

  const finishAndTranscribe = useCallback(async (blob: Blob, mimeType: string) => {
    setIsRecording(false);
    setIsTranscribing(true);
    setError(null);
    try {
      if (!blob.size) {
        throw new Error('No audio was recorded. Check your microphone.');
      }
      const base64 = await blobToBase64(blob);
      const sentence = await getTextFromVoiceInput(base64, mimeType);
      onTranscriptRef.current(sentence);
    } catch (e: any) {
      const msg = e?.message || 'Voice transcription failed.';
      setError(msg);
      appHelper.debugLog('useVoiceInput', msg);
    } finally {
      setIsTranscribing(false);
      cleanupStream();
    }
  }, [cleanupStream]);

  const stopRecording = useCallback(() => {
    const rec = mediaRecorderRef.current;
    if (!rec || rec.state === 'inactive') {
      setIsRecording(false);
      cleanupStream();
      return;
    }
    try {
      rec.stop();
    } catch {
      setIsRecording(false);
      cleanupStream();
    }
  }, [cleanupStream]);

  const startRecording = useCallback(async () => {
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
        cleanupStream();
      };
      rec.onstop = () => {
        const type = rec.mimeType || mimeType || 'audio/webm';
        const blob = new Blob(chunksRef.current, { type });
        void finishAndTranscribe(blob, type.split(';')[0] || 'audio/webm');
      };

      rec.start(250);
      setIsRecording(true);
      maxTimerRef.current = setTimeout(() => stopRecording(), MAX_RECORD_MS);
    } catch (e: any) {
      const msg =
        e?.name === 'NotAllowedError'
          ? 'Microphone permission denied.'
          : e?.message || 'Could not access the microphone.';
      setError(msg);
      cleanupStream();
    }
  }, [isListening, cleanupStream, finishAndTranscribe, stopRecording]);

  const toggle = useCallback(() => {
    if (isTranscribing) return;
    if (isRecording) stopRecording();
    else void startRecording();
  }, [isTranscribing, isRecording, stopRecording, startRecording]);

  return {
    isListening,
    isRecording,
    isTranscribing,
    /** Always true for UI — MicButton always paints; click validates browser APIs. */
    supported: true,
    browserSupported: supported,
    tenantEnabled: true,
    error,
    clearError: () => setError(null),
    toggle,
  };
}
