import React, { useEffect, useMemo, useRef, useState } from 'react';

type LargeReadOnlyTextAreaProps = {
  text: string;
  placeholder?: string;
  className?: string;
  spellCheck?: boolean;
  /** Preserve scroll position when text updates */
  preserveScroll?: boolean;
  /**
   * If `text` is very large, render only the first N characters by default to
   * keep the page responsive. User can expand to render the full text.
   */
  maxChars?: number;
};

/**
 * Performance-oriented large text viewer.
 * - Uncontrolled textarea: avoids forcing React to reconcile huge `value` on every parent re-render.
 * - Updates DOM value only when `text` changes.
 */
const LargeReadOnlyTextAreaInner: React.FC<LargeReadOnlyTextAreaProps> = ({
  text,
  placeholder,
  className,
  spellCheck = false,
  preserveScroll = true,
  maxChars = 200_000,
}) => {
  const taRef = useRef<HTMLTextAreaElement | null>(null);
  const [isExpanded, setIsExpanded] = useState(false);

  const isTruncated = !isExpanded && (text?.length ?? 0) > maxChars;
  const displayedText = useMemo(() => {
    const raw = text ?? '';
    if (!raw) return '';
    if (!isTruncated) return raw;
    const head = raw.slice(0, maxChars);
    const remaining = raw.length - head.length;
    return `${head}\n\n... (${remaining.toLocaleString()} characters truncated. Click "Show full" to render all.)`;
  }, [text, isTruncated, maxChars]);

  const initial = useMemo(() => displayedText ?? '', []); // initial defaultValue only once

  useEffect(() => {
    const ta = taRef.current;
    if (!ta) return;

    const prevTop = preserveScroll ? ta.scrollTop : 0;
    const prevLeft = preserveScroll ? ta.scrollLeft : 0;
    ta.value = displayedText ?? '';
    if (preserveScroll) {
      ta.scrollTop = prevTop;
      ta.scrollLeft = prevLeft;
    }
  }, [displayedText, preserveScroll]);

  return (
    <div className="w-full h-full min-h-0 flex flex-col">
      {isTruncated && (
        <div className="flex items-center justify-between gap-2 mb-1 flex-shrink-0">
          <div className="text-[11px] text-gray-600 min-w-0 truncate">
            Large response detected. Showing first {maxChars.toLocaleString()} characters.
          </div>
          <button
            type="button"
            className="px-2 py-1 text-[11px] rounded-[4px] border bg-white flex-shrink-0"
            onClick={() => setIsExpanded(true)}
            title="Render full text (may be slow)"
          >
            Show full
          </button>
        </div>
      )}
      {isExpanded && (text?.length ?? 0) > maxChars && (
        <div className="flex items-center justify-between gap-2 mb-1 flex-shrink-0">
          <div className="text-[11px] text-gray-600 min-w-0 truncate">
            Full text rendered ({(text?.length ?? 0).toLocaleString()} characters).
          </div>
          <button
            type="button"
            className="px-2 py-1 text-[11px] rounded-[4px] border bg-white flex-shrink-0"
            onClick={() => setIsExpanded(false)}
            title="Collapse back to preview"
          >
            Show preview
          </button>
        </div>
      )}
      <textarea
        ref={taRef}
        readOnly
        defaultValue={initial}
        placeholder={placeholder}
        className={className}
        spellCheck={spellCheck}
      />
    </div>
  );
};

export const LargeReadOnlyTextArea = React.memo(
  LargeReadOnlyTextAreaInner,
  (prev, next) =>
    prev.text === next.text &&
    prev.placeholder === next.placeholder &&
    prev.className === next.className &&
    prev.spellCheck === next.spellCheck &&
    prev.preserveScroll === next.preserveScroll,
);

