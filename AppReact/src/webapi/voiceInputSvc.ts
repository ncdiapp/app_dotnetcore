import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';

interface OperationResult<T> {
  Object?: T | null;
  object?: T | null;
  ValidationResult?: {
    Items?: Array<{ Message?: string; LocalizedMessage?: string }>;
    IsValid?: boolean;
  };
  validationResult?: {
    Items?: Array<{ Message?: string; LocalizedMessage?: string }>;
    IsValid?: boolean;
  };
  IsSuccessful?: boolean;
  isSuccessful?: boolean;
}

export interface VoiceTranscriptDto {
  Sentence?: string;
  sentence?: string;
}

/**
 * Transcribe recorded audio via server Whisper/Gemini (language auto-detected).
 * Uses relative /appai URL so CRA proxy (localhost:3000 → backend) works in dev.
 */
export async function getTextFromVoiceInput(
  audioBase64: string,
  mimeType: string
): Promise<string> {
  const url = `${endpoints.BASE_URL}/webapi/VoiceInput/GetTextFromVoiceInput`;
  const res = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ AudioBase64: audioBase64, MimeType: mimeType }),
  });
  if (!res.ok) {
    const msg = await res.text().catch(() => res.statusText);
    throw new Error(`Voice transcription failed (${res.status}): ${msg.slice(0, 300)}`);
  }
  const json = (await res.json()) as OperationResult<VoiceTranscriptDto>;
  const ok = json.IsSuccessful ?? json.isSuccessful;
  if (!ok) {
    const items = json.ValidationResult?.Items ?? json.validationResult?.Items ?? [];
    const err =
      items.map(i => i.LocalizedMessage || i.Message).filter(Boolean).join('; ')
      || 'Voice transcription failed.';
    throw new Error(err);
  }
  const obj = json.Object ?? json.object;
  const sentence = (obj?.Sentence ?? obj?.sentence ?? '').trim();
  if (!sentence) throw new Error('No speech was recognized.');
  return sentence;
}

/** Blob → raw base64 (no data: prefix). */
export function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      const comma = dataUrl.indexOf(',');
      resolve(comma >= 0 ? dataUrl.slice(comma + 1) : dataUrl);
    };
    reader.onerror = () => reject(reader.error ?? new Error('Failed to read audio'));
    reader.readAsDataURL(blob);
  });
}
