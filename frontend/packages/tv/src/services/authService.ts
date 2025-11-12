// src/services/authService.ts
import { authLogin } from '@/api/generated/auth/auth';
import { authHelpers } from '@/api/client';
import type { AuthCredentials } from '@/config/authConfig';
import { useAppStore, selectCredentials, type CredentialsState } from '@/store/useAppStore';

export type AutoLoginResult =
  | { status: 'success'; source: 'token' | 'credentials' }
  | { status: 'missing-credentials' }
  | { status: 'failure'; message?: string };

type AutoLoginInput = Partial<AuthCredentials> & Partial<CredentialsState>;

const normalizeCredentials = (
  credentials?: AutoLoginInput | null
): (AuthCredentials & { rememberMe?: boolean }) | null => {
  if (!credentials) {
    return null;
  }

  const email = credentials.email?.trim() ?? credentials.username?.trim();
  const password = credentials.password?.trim();

  if (!email || !password) {
    return null;
  }

  return {
    email,
    password,
    rememberMe: credentials.rememberMe,
  };
};

export const authService = {
  autoLogin: async (credentials?: AutoLoginInput): Promise<AutoLoginResult> => {
    try {
      // Проверяем, есть ли уже токен
      const existingToken = await authHelpers.getToken();
      if (existingToken) {
        console.log('Token already exists, skipping login');
        return { status: 'success', source: 'token' };
      }

      const storedCredentials = normalizeCredentials(selectCredentials(useAppStore.getState()));

      const providedCredentials = normalizeCredentials(credentials);

      const resolvedCredentials = storedCredentials ?? providedCredentials;

      if (!resolvedCredentials?.email || !resolvedCredentials?.password) {
        console.warn('Auto-login aborted: credentials are not available');
        return { status: 'missing-credentials' };
      }

      const rememberMe = resolvedCredentials.rememberMe ?? true;

      // Выполняем логин с безопасно полученными credentials
      console.log('Attempting auto-login...');
      const response = await authLogin({
        email: resolvedCredentials.email,
        password: resolvedCredentials.password,
        rememberMe,
      });
      console.log('Response:', response);

      // response уже содержит data напрямую (customInstance возвращает data)
      if (response?.token) {
        await authHelpers.saveToken(response.token);
        console.log('Auto-login successful');
        return { status: 'success', source: 'credentials' };
      }

      console.error('Login response did not contain token', response);
      return {
        status: 'failure',
        message: 'Не удалось получить токен авторизации',
      };
    } catch (error) {
      console.error('Auto-login failed:', error);
      return {
        status: 'failure',
        message: error instanceof Error ? error.message : 'Auto-login failed',
      };
    }
  },

  logout: async (): Promise<void> => {
    await authHelpers.removeToken();
  },

  isAuthenticated: async (): Promise<boolean> => {
    const token = await authHelpers.getToken();
    return !!token;
  },
};
