import { useSelector } from 'react-redux';
import { RootState } from '../store';
import { tw, toClassColor } from '../../helper/themeHelper';
import type { BaseTheme } from '../features/ui/theme/types';

export const useTheme = () => {
  const theme = useSelector((state: RootState) => state.theme.currentTheme);

  const t = (key: keyof BaseTheme): string => {
    const token = theme.param[key];
    const tokenCls = toClassColor(token as unknown as string);
    const keyStr = String(key);
    const isHover = keyStr.includes('_hover');
    const hoverPrefix = isHover ? 'hover:' : '';
    if (keyStr.startsWith('bg_')) return `${hoverPrefix}bg-${tokenCls}`;
    if (keyStr.startsWith('text_')) return `${hoverPrefix}text-${tokenCls}`;
    if (keyStr.startsWith('border_')) return `${hoverPrefix}border-${tokenCls}`;
    if (keyStr.startsWith('ring_')) return `${hoverPrefix}ring-${tokenCls}`;
    return `${hoverPrefix}text-${tokenCls}`;
  };

  return {
    theme,
    classes: theme, // alias for clarity in components
    param: theme.param,
    tw,
    t,
  };
};


