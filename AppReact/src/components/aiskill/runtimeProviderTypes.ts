/** Per-agent runtime / LLM provider (Agent Management). */
export const RUNTIME_PROVIDER_OPTIONS = [
  { value: '', label: 'Use Default Provider' },
  { value: 'OpenAI', label: 'OpenAI' },
  { value: 'Gemini', label: 'Gemini' },
  { value: 'Anthropic', label: 'Anthropic (Claude)' },
  { value: 'CursorCloudAgents', label: 'Cursor Cloud Agents' },
] as const;

export type RuntimeProviderValue = (typeof RUNTIME_PROVIDER_OPTIONS)[number]['value'];

export function normalizeRuntimeProvider(value: string | null | undefined, fallback = 'Gemini'): string {
  const v = (value || '').trim();
  if (!v) return '';
  const hit = RUNTIME_PROVIDER_OPTIONS.find(o => o.value.toLowerCase() === v.toLowerCase());
  if (hit) return hit.value;
  if (/^cursor/i.test(v)) return 'CursorCloudAgents';
  if (/claude/i.test(v)) return 'Anthropic';
  return '';
}

export function effectiveRuntimeProvider(value: string | null | undefined, fallback = 'Gemini'): string {
  const normalized = normalizeRuntimeProvider(value, fallback);
  return normalized || fallback;
}
