import axios, { type AxiosRequestConfig, type AxiosInstance } from 'axios';
import type { Context } from 'grammy';

import { ensureUserAccessToken, invalidateUserToken } from '@/auth';

const API_BASE_URL = process.env.API_BASE_URL ?? '/api';

let currentCtx: Context | undefined;
export function setRequestContext(ctx: Context | undefined) {
  currentCtx = ctx;
}

const client: AxiosInstance = axios.create({ baseURL: API_BASE_URL, withCredentials: true });

export async function photobankAxios<T>(config: AxiosRequestConfig, ctx?: Context): Promise<T> {
  const context = (ctx ?? currentCtx)!; // ← non-null assertion
  if (!context) throw new Error('Telegram context is required');

  const doRequest = async (force = false): Promise<T> => {
    const token = await ensureUserAccessToken(context, force); // теперь тип ОК
    const res = await client.request<T>({
      ...config,
      headers: { Authorization: `Bearer ${token}`, ...(config.headers ?? {}) },
    });
    return res.data;
  };

  try {
    return await doRequest(false);
  } catch (error) {
    const status = axios.isAxiosError(error) ? error.response?.status : undefined;
    if (status === 401 || status === 403) {
      invalidateUserToken(context);
      return await doRequest(true);
    }
    throw error;
  }
}
