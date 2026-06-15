import { useCallback, useRef, useState } from 'react';

/**
 * Wraps the Web Speech API for voice-to-text input.
 *
 * Usage:
 *   const voice = useVoiceInput((transcript) => setInput(prev => prev ? `${prev} ${transcript}` : transcript));
 *   // render: <button onClick={voice.toggle} disabled={!voice.supported} />
 */
export function useVoiceInput(onTranscript: (text: string) => void) {
  const [isListening, setIsListening] = useState(false);
  const recognitionRef = useRef<any>(null);

  const supported =
    typeof window !== 'undefined' &&
    ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window);

  const toggle = useCallback(() => {
    if (!supported) return;

    if (isListening) {
      recognitionRef.current?.stop();
      return;
    }

    const SR: any =
      (window as any).SpeechRecognition ?? (window as any).webkitSpeechRecognition;
    const rec = new SR();
    rec.lang = 'en-US';
    rec.interimResults = false;
    rec.maxAlternatives = 1;

    rec.onresult = (e: any) => {
      const transcript: string = e.results[0][0].transcript;
      onTranscript(transcript);
    };
    rec.onerror = () => setIsListening(false);
    rec.onend   = () => setIsListening(false);

    recognitionRef.current = rec;
    rec.start();
    setIsListening(true);
  }, [isListening, supported, onTranscript]);

  return { isListening, supported, toggle };
}
