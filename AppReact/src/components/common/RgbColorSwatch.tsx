import React from 'react';
import { parseRgbColorDisplayValue } from '../../helper/rgbColorDisplayHelper';

interface RgbColorSwatchProps {
  value: unknown;
  height?: number;
  className?: string;
  showFallbackText?: boolean;
}

export const RgbColorSwatch: React.FC<RgbColorSwatchProps> = ({
  value,
  height = 20,
  className = '',
  showFallbackText = false,
}) => {
  const text = value != null ? String(value).trim() : '';
  const cssColor = parseRgbColorDisplayValue(value);

  return (
    <div
      className={className}
      title={text || undefined}
      style={{
        display: 'flex',
        alignItems: 'center',
        width: '100%',
        height: '100%',
      }}
    >
      <div
        style={{
          width: '100%',
          height,
          backgroundColor: cssColor ?? 'transparent',
        }}
      />
      {showFallbackText && !cssColor && text ? (
        <span className="ml-2 truncate text-xs">{text}</span>
      ) : null}
    </div>
  );
};

export default RgbColorSwatch;
