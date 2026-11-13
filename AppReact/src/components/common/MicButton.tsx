import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useVoiceInput } from '../../hooks/useVoiceInput';

export type MicButtonProps = {
  onTranscript: (text: string) => void;
  disabled?: boolean;
  className?: string;
  tenantEnabled?: boolean;
  title?: string;
};

/**
 * Always visible mic next to Send. Click starts/stops MediaRecorder → Whisper/Gemini STT.
 * Errors are shown under the button so failures are not silent.
 */
export function MicButton({
  onTranscript,
  disabled,
  className,
  title,
}: MicButtonProps) {
  const { theme } = useTheme();
  const voice = useVoiceInput(onTranscript);

  const busy = voice.isListening;
  const tip =
    title
    ?? (voice.error
      ? voice.error
      : voice.isTranscribing
        ? 'Transcribing…'
        : voice.isRecording
          ? 'Stop recording'
          : 'Voice input');

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
      >
        <i
          className={[
            'fa-solid text-xs',
            voice.isTranscribing
              ? 'fa-spinner fa-spin'
              : voice.isRecording
                ? 'fa-stop'
                : 'fa-microphone',
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
