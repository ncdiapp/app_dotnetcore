import { getHeaders } from '../helper/apiServiceHelper';
import { buildEndpointUrl } from './endpoints';

export interface AiActionInput {
  inputType: 'text' | 'image';
  textValue?: string;
  imageBase64?: string;
  mimeType?: string;
}

export interface AiActionResult {
  rawJson: string;
  warnings?: string;
}

export async function callAiAction(
  inputs: AiActionInput[],
  skillName: string
): Promise<AiActionResult> {
  const url = buildEndpointUrl('/api/ai/action');
  const res = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ inputs, skillName }),
  });
  if (!res.ok) {
    const msg = await res.text().catch(() => res.statusText);
    throw new Error(`AI action failed (${res.status}): ${msg}`);
  }
  return res.json();
}

/** Convert a File object to base64 string (strips the data: prefix). */
export async function fileToBase64(file: File): Promise<{ base64: string; mimeType: string }> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      const [header, base64] = dataUrl.split(',');
      const mimeType = header.match(/:(.*?);/)?.[1] ?? file.type ?? 'image/jpeg';
      resolve({ base64, mimeType });
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

/**
 * Parse an image value from a form's DictOneToOneFields.
 * The platform stores image fields as data URLs ("data:image/jpeg;base64,...").
 * Returns { base64, mimeType } or null if the value is empty / not an image.
 */
export function parseImageFieldValue(rawValue: unknown): { base64: string; mimeType: string } | null {
  if (!rawValue || typeof rawValue !== 'string') return null;
  const dataUrl = rawValue.trim();
  if (!dataUrl.startsWith('data:')) return null;
  const [header, base64] = dataUrl.split(',');
  if (!base64) return null;
  const mimeType = header.match(/:(.*?);/)?.[1] ?? 'image/jpeg';
  return { base64, mimeType };
}
