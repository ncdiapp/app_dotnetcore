const clamp255 = (part: string): number | null => {
  const n = Number(part);
  if (!Number.isFinite(n)) return null;
  return Math.max(0, Math.min(255, Math.round(n)));
};

const clampAlpha = (part: string): number | null => {
  const n = Number(part);
  if (!Number.isFinite(n)) return null;
  return Math.max(0, Math.min(1, n > 1 ? n / 255 : n));
};

/** Parse RGBColorDisplay values (e.g. "64|255|255") into a CSS color string. */
export const parseRgbColorDisplayValue = (raw: unknown): string | null => {
  if (raw == null) return null;

  const text = String(raw).trim();
  if (!text) return null;

  if (/^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/.test(text)) {
    return text;
  }

  if (/^rgba?\(/i.test(text)) {
    return text;
  }

  if (text.includes('|')) {
    const parts = text.split('|').map((part) => part.trim()).filter((part) => part.length > 0);
    if (parts.length >= 3) {
      const r = clamp255(parts[0]);
      const g = clamp255(parts[1]);
      const b = clamp255(parts[2]);
      if (r != null && g != null && b != null) {
        if (parts.length >= 4) {
          const a = clampAlpha(parts[3]);
          return a != null ? `rgba(${r}, ${g}, ${b}, ${a})` : `rgb(${r}, ${g}, ${b})`;
        }
        return `rgb(${r}, ${g}, ${b})`;
      }
    }
  }

  if (text.includes(',')) {
    const parts = text.split(',').map((part) => part.trim());
    if (parts.length >= 3) {
      const r = clamp255(parts[0]);
      const g = clamp255(parts[1]);
      const b = clamp255(parts[2]);
      if (r != null && g != null && b != null) {
        return `rgb(${r}, ${g}, ${b})`;
      }
    }
  }

  return null;
};
