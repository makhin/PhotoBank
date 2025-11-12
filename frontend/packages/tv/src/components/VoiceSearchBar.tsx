import React, { useCallback } from 'react';
import { Platform, Text, TextInput, View } from 'react-native';

import { FocusableButton } from '@/components/FocusableButton';
import { useThemedStyles } from '@/hooks/useThemedStyles';
import { useVoiceSearch } from '@/hooks/useVoiceSearch';

export const VoiceSearchBar: React.FC = () => {
  const { styles: appStyles, colors } = useThemedStyles();
  const {
    query,
    setQuery,
    isListening,
    isVoiceSupported,
    error,
    inputRef,
    handleVoiceSearchPress,
  } = useVoiceSearch({ locale: 'ru-RU' });

  const handleChangeText = useCallback(
    (text: string) => {
      setQuery(text);
    },
    [setQuery],
  );

  return (
    <View style={[appStyles.layout.column, appStyles.gaps.sm]}>
      <View style={[appStyles.layout.rowBetween, appStyles.gaps.md, appStyles.search.container]}>
        <TextInput
          ref={inputRef}
          style={[appStyles.forms.input, appStyles.search.input]}
          value={query}
          onChangeText={handleChangeText}
          placeholder="Поиск по фото"
          placeholderTextColor={colors.placeholderText}
          returnKeyType="search"
          accessibilityLabel="Строка поиска"
        />
        {isVoiceSupported ? (
          <FocusableButton
            title={isListening ? '⏹️' : '🎤'}
            onPress={() => void handleVoiceSearchPress()}
            hasTVPreferredFocus={Platform.isTV}
            style={[appStyles.buttons.iconSquare, appStyles.search.microphoneButton]}
            textStyle={appStyles.buttons.iconText}
            accessibilityLabel="Голосовой поиск"
            accessibilityHint={
              isListening ? 'Остановить запись и выполнить поиск' : 'Начать запись голосового запроса'
            }
          />
        ) : null}
      </View>
      {error ? (
        <Text style={[appStyles.text.bodySubtle, { color: colors.danger }]} accessibilityRole="alert">
          {error}
        </Text>
      ) : null}
    </View>
  );
};
