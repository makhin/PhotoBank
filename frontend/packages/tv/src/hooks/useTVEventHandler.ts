// src/hooks/useTVEventHandler.ts
import { useEffect } from 'react';
import { Platform, BackHandler } from 'react-native';

type TVEventHandler = (evt: any) => boolean;

export const useTVEventHandler = (handler: TVEventHandler) => {
  useEffect(() => {
    if (!Platform.isTV) return;

    // Для TV используем BackHandler как альтернативу
    // TVEventHandler может не работать корректно в новых версиях RN
    const backHandler = BackHandler.addEventListener('hardwareBackPress', () => {
      // Эмулируем событие для back button
      return handler({ eventType: 'back' });
    });

    console.log('TV event handler registered (using BackHandler)');

    return () => {
      backHandler.remove();
      console.log('TV event handler unregistered');
    };
  }, [handler]);
};
