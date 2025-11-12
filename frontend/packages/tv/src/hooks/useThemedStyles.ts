import { useMemo } from 'react';

import { createAppStyles, darkColors, lightColors, type AppStyles, type ColorPalette } from '@/styles';
import { selectTheme, useAppStore } from '@/store/useAppStore';

type ThemeMode = 'light' | 'dark';

interface UseThemedStylesResult {
  theme: ThemeMode;
  colors: ColorPalette;
  styles: AppStyles;
}

export const useThemedStyles = (): UseThemedStylesResult => {
  const theme = useAppStore(selectTheme);
  const palette = theme === 'dark' ? darkColors : lightColors;

  const styles = useMemo(() => createAppStyles(palette), [theme]);

  return { theme, colors: palette, styles };
};
