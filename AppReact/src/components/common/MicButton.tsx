import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useVoiceInput, type VoiceEngine } from '../../hooks/useVoiceInput';

export type MicButtonProps = {
  onTranscript: (text: string) => void;
  disabled?: boolean;
  className?: string;
  tenantEnabled?: boolean;
  title?: string;
};

/** Idle icon differs by engine so power users can tell which path is active. */
function idleMicIcon(engine: VoiceEngine): string {
  // Whisper (server): classic mic
  // Web Speech (browser fallback): mic-lines (waveform)
  if (engine === 'webspeech') return 'fa-microphone-lines';
  return 'fa-microphone';
}

function engineLabel(engine: VoiceEngine): string {
  if (engine === 'whisper') return 'Voice input (Whisper)';
  if (engine === 'webspeech') return 'Voice input (browser)';
  return 'Voice input unavailable';
}

/**
 * Agent mic: Whisper when tenant server voice is enabled; otherwise Web Speech fallback.
 * Icons differ slightly by engine (`fa-microphone` vs `fa-microphone-lines`).
 */
export function MicButton({
  onTranscript,
  disabled,
  className,
  tenantEnabled,
  title,
}: MicButtonProps) {
  const { theme } = useTheme();
  const voice = useVoiceInput(onTranscript, { tenantEnabled });

  if (!voice.supported) return null;

  const busy = voice.isListening;
  const tip =
    title
    ?? (voice.error
      ? voice.error
      : voice.isTranscribing
        ? 'Transcribing…'
        : voice.isRecording
          ? 'Stop'
          : engineLabel(voice.engine));

  return (
    <div className="relative flex flex-col items-center shrink-0">
      <button
        type="button"
        onClick={() => {
          voice.clearError();
          voice.toggle();
        }}
        disabled={!!disabled || voice.isTranscribing}
        className={[
          'inline-flex items-center justify-center shrink-0 transition-colors',
          'disabled:opacity-40 disabled:cursor-not-allowed',
          busy ? 'bg-red-500 hover:bg-red-600 text-white animate-pulse' : theme.button_default,
          className || 'w-8 h-8 rounded-[4px]',
        ].join(' ')}
        title={tip}
        aria-label={tip}
        aria-pressed={voice.isRecording}
        data-testid="agent-mic-button"
        data-voice-engine={voice.engine}
      >
        <i
          className={[
            'fa-solid text-xs',
            voice.isTranscribing
              ? 'fa-spinner fa-spin'
              : voice.isRecording
                ? 'fa-stop'
                : idleMicIcon(voice.engine),
          ].join(' ')}
          aria-hidden
        />
      </button>
      {voice.error && (
        <div
          className="absolute bottom-full mb-1 right-0 z-20 max-w-[240px] px-2 py-1 text-[10px] leading-snug rounded border border-red-300 bg-red-50 text-red-700 shadow"
          role="alert"
        >
          {voice.error}
        </div>
      )}
    </div>
  );
}

export default MicButton;
