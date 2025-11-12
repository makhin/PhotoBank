// src/api/client.ts
import axios, { AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

const STORAGE_TOKEN_KEY = 'auth_token';

// Создаём экземпляр axios
export const axiosInstance = axios.create({
  baseURL: 'http://192.168.1.63:5066', // TODO: Настройте базовый URL
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor для добавления токена авторизации
axiosInstance.interceptors.request.use(
  async (config) => {
    try {
      const token = await AsyncStorage.getItem(STORAGE_TOKEN_KEY);
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    } catch (error) {
      console.error('Error getting auth token:', error);
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor для обработки ответов и ошибок
axiosInstance.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Токен истёк или невалиден - очищаем и можно перенаправить на логин
      await AsyncStorage.removeItem(STORAGE_TOKEN_KEY);
      // Здесь можно добавить навигацию к экрану логина
    }
    return Promise.reject(error);
  }
);

// Кастомный instance для Orval
export const customInstance = <T>(
  config: AxiosRequestConfig,
  options?: AxiosRequestConfig
): Promise<T> => {
  const source = axios.CancelToken.source();

  const promise = axiosInstance({
    ...config,
    ...options,
    cancelToken: source.token,
  }).then(({ data }: AxiosResponse<T>) => data);

  // @ts-expect-error - добавляем cancel для возможности отмены запроса
  promise.cancel = () => {
    source.cancel('Query was cancelled');
  };

  return promise;
};

// Helper функции для работы с токеном
export const authHelpers = {
  saveToken: async (token: string): Promise<void> => {
    try {
      await AsyncStorage.setItem(STORAGE_TOKEN_KEY, token);
    } catch (error) {
      console.error('Error saving auth token:', error);
    }
  },

  getToken: async (): Promise<string | null> => {
    try {
      return await AsyncStorage.getItem(STORAGE_TOKEN_KEY);
    } catch (error) {
      console.error('Error getting auth token:', error);
      return null;
    }
  },

  removeToken: async (): Promise<void> => {
    try {
      await AsyncStorage.removeItem(STORAGE_TOKEN_KEY);
    } catch (error) {
      console.error('Error removing auth token:', error);
    }
  },
};

export default customInstance;
