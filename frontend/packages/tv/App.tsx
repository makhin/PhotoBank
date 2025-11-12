// App.tsx
import React from 'react';
import { StatusBar, Platform } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { QueryClientProvider } from '@tanstack/react-query';
import { RootNavigator } from './src/navigation/RootNavigator';
import { queryClient } from '@/api';
import { useThemedStyles } from '@/hooks/useThemedStyles';

const App: React.FC = () => {
  const { theme, colors } = useThemedStyles();

  return (
    <QueryClientProvider client={queryClient}>
      <SafeAreaProvider>
        <StatusBar
          barStyle={theme === 'dark' || Platform.isTV ? 'light-content' : 'dark-content'}
          backgroundColor={colors.background}
          hidden={Platform.isTV}
        />
        <RootNavigator />
      </SafeAreaProvider>
    </QueryClientProvider>
  );
};

export default App;
