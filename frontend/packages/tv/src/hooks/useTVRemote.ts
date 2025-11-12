// src/hooks/useTVRemote.ts
import { useEffect } from 'react';
import { Platform, BackHandler } from 'react-native';

type TVEventType = 'up' | 'down' | 'left' | 'right' | 'select' | 'menu' | 'playPause';

interface TVRemoteHandler {
  onUp?: () => void;
  onDown?: () => void;
  onLeft?: () => void;
  onRight?: () => void;
  onSelect?: () => void;
  onMenu?: () => void;
  onPlayPause?: () => void;
  onBack?: () => boolean;
}

export const useTVRemote = (handlers: TVRemoteHandler) => {
  useEffect(() => {
    if (!Platform.isTV) return;

    // ��������� Back ������ �� Android TV
    const backHandler = BackHandler.addEventListener('hardwareBackPress', () => {
      if (handlers.onBack) {
        return handlers.onBack();
      }
      return false;
    });

    // ������ TVEventHandler ������ ��� TV ��������
    let TVEventHandler: any;
    try {
      const rntvos = require('react-native-tvos');
      TVEventHandler = rntvos.useTVEventHandler;
    } catch (e) {
      console.warn('TVEventHandler not available');
      return;
    }

    if (TVEventHandler) {
      const handleTVEvent = (evt: { eventType: TVEventType }) => {
        switch (evt.eventType) {
          case 'up':
            handlers.onUp?.();
            break;
          case 'down':
            handlers.onDown?.();
            break;
          case 'left':
            handlers.onLeft?.();
            break;
          case 'right':
            handlers.onRight?.();
            break;
          case 'select':
            handlers.onSelect?.();
            break;
          case 'menu':
            handlers.onMenu?.();
            break;
          case 'playPause':
            handlers.onPlayPause?.();
            break;
        }
      };

      TVEventHandler(handleTVEvent);
    }

    return () => {
      backHandler.remove();
    };
  }, [handlers]);
};
