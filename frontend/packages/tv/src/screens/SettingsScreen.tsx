// src/screens/SettingsScreen.tsx
import React, { useEffect, useState } from 'react';
import { View, Text, TextInput, Alert } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '@/navigation/RootNavigator';
import { FocusableButton } from '@/components/FocusableButton';
import { useAppStore } from '@/store/useAppStore';
import { authService } from '@/services/authService';
import { personsGetAll } from '@/api/generated/persons/persons';
import { getTags } from '@/api/generated/tags/tags';
import { useThemedStyles } from '@/hooks/useThemedStyles';

type Props = NativeStackScreenProps<RootStackParamList, 'Settings'>;

const SettingsScreen: React.FC<Props> = ({ navigation }) => {
  const setTheme = useAppStore((state) => state.setTheme);
  const storedUsername = useAppStore((state) => state.credentials.username);
  const storedPassword = useAppStore((state) => state.credentials.password);
  const persistCredentials = useAppStore((state) => state.setCredentials);
  const setPersons = useAppStore((state) => state.setPersons);
  const setTags = useAppStore((state) => state.setTags);
  const { theme, styles: appStyles, colors } = useThemedStyles();

  const [username, setUsername] = useState(storedUsername);
  const [password, setPassword] = useState(storedPassword);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    setUsername(storedUsername);
    setPassword(storedPassword);
  }, [storedUsername, storedPassword]);

  const handleThemeToggle = () => {
    setTheme(theme === 'dark' ? 'light' : 'dark');
  };

  const handleSaveCredentials = async () => {
    const normalizedUsername = username.trim();
    if (!normalizedUsername) {
      Alert.alert('Ошибка', 'Имя пользователя не может быть пустым.');
      return;
    }

    if (!password) {
      Alert.alert('Ошибка', 'Пароль не может быть пустым.');
      return;
    }

    setIsLoading(true);

    try {
      // Сохраняем credentials в store
      persistCredentials({
        username: normalizedUsername,
        password,
      });

      // Пытаемся залогиниться
      const authResult = await authService.autoLogin({
        username: normalizedUsername,
        password,
      });

      if (authResult.status === 'success') {
        // Загружаем справочники
        try {
          const [personsData, tagsData] = await Promise.all([personsGetAll(), getTags()]);
          setPersons(personsData);
          setTags(tagsData);
        } catch (error) {
          console.error('Error loading reference data:', error);
        }

        // Переходим на главный экран
        navigation.replace('Home');
      } else {
        const message =
          authResult.message ?? 'Не удалось авторизоваться. Проверьте учетные данные.';
        Alert.alert('Ошибка авторизации', message);
      }
    } catch (error) {
      console.error('Error during login:', error);
      Alert.alert('Ошибка', 'Произошла ошибка при авторизации.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <View style={appStyles.layout.screen}>
      <View style={appStyles.insets.screenPadding}>
        {/* Theme Section */}
        <View style={appStyles.sections.container}>
          <Text style={appStyles.text.headingPrimary}>Тема</Text>
          <Text style={appStyles.forms.infoText}>
            Текущая тема: {theme === 'dark' ? 'Тёмная' : 'Светлая'}
          </Text>
          <View style={appStyles.insets.marginTopSm}>
            <FocusableButton
              title={`Переключить на ${theme === 'dark' ? 'светлую' : 'тёмную'}`}
              onPress={handleThemeToggle}
              hasTVPreferredFocus={true}
            />
          </View>
        </View>

        {/* Credentials Section */}
        <View style={appStyles.sections.container}>
          <Text style={appStyles.text.headingPrimary}>Учетные данные</Text>
          <View style={appStyles.forms.group}>
            <Text style={appStyles.forms.label}>Имя пользователя</Text>
            <TextInput
              focusable
              style={appStyles.forms.input}
              value={username}
              onChangeText={setUsername}
              placeholder="Введите имя пользователя"
              placeholderTextColor={colors.textMuted}
              autoCapitalize="none"
            />
          </View>
          <View style={appStyles.forms.group}>
            <Text style={appStyles.forms.label}>Пароль</Text>
            <TextInput
              focusable
              style={appStyles.forms.input}
              value={password}
              onChangeText={setPassword}
              placeholder="Введите пароль"
              placeholderTextColor={colors.textMuted}
              secureTextEntry
            />
          </View>
          <View style={appStyles.insets.marginTopSm}>
            <FocusableButton
              title={isLoading ? 'Вход...' : 'Сохранить'}
              onPress={handleSaveCredentials}
              disabled={isLoading}
            />
          </View>
        </View>
      </View>
    </View>
  );
};

export default SettingsScreen;
