import axios, { AxiosRequestConfig, AxiosError } from 'axios';
import type { Context } from 'grammy';

import { ensureUserAccessToken, invalidateUserToken } from '../auth';

const API_BASE_URL = process.env.API_BASE_URL;

let currentCtx: Context | undefined;

export function setRequestContext(ctx: Context) {
  currentCtx = ctx;
}

export async function photobankAxios<T>(config: AxiosRequestConfig, ctx?: Context) {
  const context = ctx ?? currentCtx;
  if (!context) {
    throw new Error('Telegram context is required');
  }
  if (!API_BASE_URL) {
    throw new Error('API_BASE_URL is not set');
  }
  async function doRequest(force = false) {
    const token = await ensureUserAccessToken(context, force);
    return axios<T>({
      baseURL: API_BASE_URL,
      headers: { Authorization: `Bearer ${token}`, ...(config.headers || {}) },
      ...config,
    });
  }
  try {
    return await doRequest(false);
  } catch (err) {
    const status = (err as AxiosError | undefined)?.response?.status;
    if (status === 401 || status === 403) {
      // токен протух или права изменились — инвалидируем и пробуем ещё раз
      invalidateUserToken(context);
      return await doRequest(true);
    }
    throw err;
  }
}
