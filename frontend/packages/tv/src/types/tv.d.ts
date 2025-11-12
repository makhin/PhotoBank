// src/types/tv.d.ts

import { Platform } from 'react-native';

declare module 'react-native' {
  interface PlatformStatic {
    isTV: boolean;
    isTVOS: boolean;
  }

  interface PressableProps {
    onFocus?: (event: any) => void;
    onBlur?: (event: any) => void;
    hasTVPreferredFocus?: boolean;
    nextFocusDown?: number | null;
    nextFocusForward?: number | null;
    nextFocusLeft?: number | null;
    nextFocusRight?: number | null;
    nextFocusUp?: number | null;
  }

  interface TouchableOpacityProps {
    onFocus?: (event: any) => void;
    onBlur?: (event: any) => void;
    hasTVPreferredFocus?: boolean;
    nextFocusDown?: number | null;
    nextFocusForward?: number | null;
    nextFocusLeft?: number | null;
    nextFocusRight?: number | null;
    nextFocusUp?: number | null;
  }
}

declare module 'react-native-tvos' {
  export interface TVFocusGuideViewProps {
    destinations?: any[];
    autoFocus?: boolean;
    trapFocusUp?: boolean;
    trapFocusDown?: boolean;
    trapFocusLeft?: boolean;
    trapFocusRight?: boolean;
    style?: any;
    children?: React.ReactNode;
  }

  export const TVFocusGuideView: React.ComponentType<TVFocusGuideViewProps>;

  export interface TVEventHandler {
    enable: () => void;
    disable: () => void;
  }

  export interface TVEventInfo {
    eventType: 'up' | 'down' | 'left' | 'right' | 'select' | 'menu' | 'playPause';
    eventKeyAction?: number;
  }

  export function useTVEventHandler(handler: (event: TVEventInfo) => void): void;
}
