import axios, { type AxiosRequestConfig, type AxiosInstance } from 'axios';
import type { Context } from 'grammy';

import { ensureUserAccessToken, invalidateUserToken } from '@/auth';

const API_BASE_URL = process.env.API_BASE_URL ?? '/api';

let currentCtx: Context | undefined;
export function setRequestContext(ctx: Context | undefined) {
  currentCtx = ctx;
}

const client: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
});

// Перегрузки: orval будет использовать первую
export async function photobankAxios<T>(config: AxiosRequestConfig): Promise<T>;
export async function photobankAxios<T>(config: AxiosRequestConfig, ctx: Context): Promise<T>;
export async function photobankAxios<T>(config: AxiosRequestConfig, ctx?: Context): Promise<T> {
  const context = ctx ?? currentCtx;
  if (!context) {
    throw new Error('Telegram context is required');
  }

  async function doRequest(force = false): Promise<T> {
    const token = await ensureUserAccessToken(context, force);
    const res = await client.request<T>({
      ...config,
      // axios v1+ понимает AbortSignal: signal?: AbortSignal
      headers: {
        Authorization: `Bearer ${token}`,
        ...(config.headers ?? {}),
      },
    });
    return res.data; // важно: возвращаем тело, не AxiosResponse
  }

  try {
    return await doRequest(false);
  } catch (error) {
    const err = error as unknown;
    const status = (axios.isAxiosError(err) ? err.response?.status : undefined);

    if (status === 401 || status === 403) {
      // токен протух/права изменились — инвалидируем и пробуем ещё раз
      invalidateUserToken(context);
      return await doRequest(true);
    }
    throw error;
  }
}
