import React, { useMemo } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

type IframePopupProps = {
  open: boolean;
  title: string;
  iframeSrc: string;
  width?: number | null;
  height?: number | null;
  onClose: () => void;
};

const clamp = (n: number, min: number, max: number) => Math.max(min, Math.min(max, n));

export default function IframePopup({
  open,
  title,
  iframeSrc,
  width,
  height,
  onClose,
}: IframePopupProps) {
  const { theme } = useTheme();

  const style = useMemo(() => {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 1200;
    const vh = typeof window !== 'undefined' ? window.innerHeight : 800;

    // Reasonable defaults matching Angular popup sizes in this repo.
    const w = width != null ? Number(width) : 1160;
    const h = height != null ? Number(height) : 780;

    return {
      width: `${clamp(w, 320, Math.floor(vw * 0.95))}px`,
      height: `${clamp(h, 240, Math.floor(vh * 0.88))}px`,
      maxWidth: '95vw',
      maxHeight: '88vh',
    } as React.CSSProperties;
  }, [width, height]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[10020] flex items-center justify-center bg-black/40"
      onClick={onClose}
      role="presentation"
    >
      <div
        className={`${theme.mainContentSection} rounded-md shadow-xl border flex flex-col overflow-hidden`}
        style={style}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`} title={title}>
            {title || 'Popup'}
          </div>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={onClose}
            title="Close"
          >
            <i className="fa-solid fa-xmark mr-1" aria-hidden /> Close
          </button>
        </div>
        <div className="w-full h-1 flex-none bg-transparent" />
        <div className="w-full h-full flex-auto overflow-hidden">
          <iframe src={iframeSrc} title={title} className="w-full h-full border-0" />
        </div>
      </div>
    </div>
  );
}

