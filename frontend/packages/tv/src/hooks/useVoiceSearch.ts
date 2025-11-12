import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert,
  PermissionsAndroid,
  Platform,
  TextInput,
  ToastAndroid,
} from 'react-native';
import Voice, {
  SpeechErrorEvent,
  SpeechResultsEvent,
} from '@react-native-voice/voice';

interface UseVoiceSearchOptions {
  locale?: string;
}

interface UseVoiceSearchResult {
  query: string;
  setQuery: (value: string) => void;
  isListening: boolean;
  isVoiceSupported: boolean;
  error: string | null;
  inputRef: React.RefObject<TextInput | null>;
  handleVoiceSearchPress: () => Promise<void>;
}

export const useVoiceSearch = (
  { locale = 'ru-RU' }: UseVoiceSearchOptions = {},
): UseVoiceSearchResult => {
  const inputRef = useRef<TextInput | null>(null);
  const [query, setQuery] = useState('');
  const [isListening, setIsListening] = useState(false);
  const [isVoiceSupported, setIsVoiceSupported] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const configureVoice = async () => {
      try {
        const available = await Voice.isAvailable();

        if (isMounted) {
          setIsVoiceSupported(Boolean(available));
        }
      } catch (err) {
        if (isMounted) {
          setIsVoiceSupported(false);
        }
      }
    };

    configureVoice();

    Voice.onSpeechStart = () => {
      setIsListening(true);
      setError(null);
    };

    Voice.onSpeechEnd = () => {
      setIsListening(false);
    };

    Voice.onSpeechResults = (event: SpeechResultsEvent) => {
      const [transcript] = event.value ?? [];
      if (typeof transcript === 'string') {
        setQuery(transcript);
      }
    };

    Voice.onSpeechError = (event: SpeechErrorEvent) => {
      const message = event.error?.message ?? 'Ошибка голосового поиска';
      setError(message);
      setIsListening(false);
    };

    return () => {
      isMounted = false;
      Voice.destroy().then(Voice.removeAllListeners);
    };
  }, []);

  useEffect(() => {
    if (!error) {
      return;
    }

    if (Platform.OS === 'android') {
      ToastAndroid.show(error, ToastAndroid.SHORT);
    } else {
      Alert.alert('Голосовой поиск', error);
    }
  }, [error]);

  const requestMicrophonePermission = useCallback(async () => {
    if (Platform.OS !== 'android' || Platform.isTV) {
      return true;
    }

    try {
      const permission = PermissionsAndroid.PERMISSIONS.RECORD_AUDIO;

      if (!permission) {
        setError('Разрешение на использование микрофона недоступно.');
        return false;
      }

      const result = await PermissionsAndroid.request(permission, {
        title: 'Доступ к микрофону',
        message: 'Приложению нужен доступ к микрофону для голосового поиска.',
        buttonPositive: 'Разрешить',
        buttonNegative: 'Отмена',
      });

      const granted = result === PermissionsAndroid.RESULTS.GRANTED;

      if (!granted) {
        setError('Доступ к микрофону не предоставлен.');
      }

      return granted;
    } catch (err) {
      setError('Не удалось запросить доступ к микрофону.');
      return false;
    }
  }, []);

  const handleVoiceSearchPress = useCallback(async () => {
    if (!isVoiceSupported && !isListening) {
      return;
    }

    if (isListening) {
      try {
        await Voice.stop();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Не удалось остановить голосовой поиск.');
      }
      return;
    }

    const hasPermission = await requestMicrophonePermission();

    if (!hasPermission) {
      return;
    }

    try {
      setError(null);
      setQuery('');
      inputRef.current?.clear?.();
      inputRef.current?.focus?.();
      await Voice.start(locale);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось запустить голосовой поиск.');
    }
  }, [isVoiceSupported, isListening, requestMicrophonePermission, locale]);

  return {
    query,
    setQuery,
    isListening,
    isVoiceSupported,
    error,
    inputRef,
    handleVoiceSearchPress,
  };
};
