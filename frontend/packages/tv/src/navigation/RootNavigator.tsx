// src/navigation/RootNavigator.tsx
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { DarkTheme, DefaultTheme, NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Platform, View, Text, ActivityIndicator, Alert } from 'react-native';

import HomeScreen from '@/screens/HomeScreen';
import DetailScreen from '@/screens/DetailScreen';
import MetadataScreen from '@/screens/MetadataScreen';
import SettingsScreen from '@/screens/SettingsScreen';
import { authService } from '@/services/authService';
import { useAppStore } from '@/store/useAppStore';
import { personsGetAll } from '@/api/generated/persons/persons';
import { getTags } from '@/api/generated/tags/tags';
import { useThemedStyles } from '@/hooks/useThemedStyles';

export type RootStackParamList = {
  Home: undefined;
  Detail: { photoId: number; photoIds: number[] };
  Metadata: { photoId: number; photoIds: number[] };
  Settings: undefined;
};

const Stack = createNativeStackNavigator<RootStackParamList>();

export const RootNavigator: React.FC = () => {
  const [isLoading, setIsLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);
  const [initialRouteName, setInitialRouteName] = useState<keyof RootStackParamList>('Home');
  const setPersons = useAppStore((state) => state.setPersons);
  const setTags = useAppStore((state) => state.setTags);
  const username = useAppStore((state) => state.credentials.username);
  const password = useAppStore((state) => state.credentials.password);
  const { styles: appStyles, colors, theme } = useThemedStyles();

  const navigationTheme = useMemo(() => {
    const baseTheme = theme === 'dark' ? DarkTheme : DefaultTheme;

    return {
      ...baseTheme,
      colors: {
        ...baseTheme.colors,
        background: colors.background,
        card: colors.backgroundMuted,
        primary: colors.accentPrimary,
        text: colors.textPrimary,
        border: colors.borderMuted,
        notification: colors.accentStrong,
      },
    };
  }, [theme, colors]);

  const loadReferenceData = useCallback(async () => {
    try {
      // Загружаем persons и tags параллельно
      const [personsData, tagsData] = await Promise.all([personsGetAll(), getTags()]);

      console.log('Loaded persons:', personsData.length);
      console.log('Loaded tags:', tagsData.length);

      setPersons(personsData);
      setTags(tagsData);
    } catch (error) {
      console.error('Error loading reference data:', error);
      // Не блокируем приложение, если не удалось загрузить справочники
    }
  }, [setPersons, setTags]);

  const initializeApp = useCallback(async () => {
    try {
      setIsLoading(true);
      setAuthError(null);

      const normalizedUsername = username?.trim();
      const normalizedPassword = password?.trim();
      const hasStoredCredentials = Boolean(normalizedUsername && normalizedPassword);

      const alreadyAuthenticated = await authService.isAuthenticated();

      if (!hasStoredCredentials && !alreadyAuthenticated) {
        setInitialRouteName('Settings');
        return;
      }

      if (alreadyAuthenticated) {
        setInitialRouteName('Home');
        await loadReferenceData();
        return;
      }

      // 1. Авторизация
      const authResult = await authService.autoLogin({ username, password });

      if (authResult.status === 'success') {
        setInitialRouteName('Home');
        await loadReferenceData();
        return;
      }

      if (authResult.status === 'missing-credentials') {
        setInitialRouteName('Settings');
        return;
      }

      setInitialRouteName('Settings');
      const message = authResult.message ?? 'Не удалось авторизоваться. Проверьте учетные данные.';
      Alert.alert('Ошибка авторизации', message);
    } catch (error) {
      console.error('App initialization error:', error);
      setAuthError('Ошибка инициализации');
    } finally {
      setIsLoading(false);
    }
  }, [username, password, loadReferenceData]);

  useEffect(() => {
    initializeApp();
  }, [initializeApp]);

  if (isLoading) {
    return (
      <View style={appStyles.layout.centeredScreen}>
        <ActivityIndicator size="large" color={colors.accentPrimary} />
        <Text style={appStyles.text.status}>Загрузка...</Text>
      </View>
    );
  }

  if (authError) {
    return (
      <View style={[appStyles.layout.centeredScreen, appStyles.insets.sectionPadding]}>
        <Text style={appStyles.text.error}>{authError}</Text>
      </View>
    );
  }

  return (
    <NavigationContainer theme={navigationTheme}>
      <Stack.Navigator
        initialRouteName={initialRouteName}
        screenOptions={{
          headerShown: false,
          animation: Platform.isTV ? 'fade' : 'default',
        }}
      >
        <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Photobank' }} />
        <Stack.Screen name="Detail" component={DetailScreen} options={{ title: 'Photo' }} />
        <Stack.Screen
          name="Metadata"
          component={MetadataScreen}
          options={{ title: 'Photo Metadata' }}
        />
        <Stack.Screen name="Settings" component={SettingsScreen} options={{ title: 'Settings' }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
};
