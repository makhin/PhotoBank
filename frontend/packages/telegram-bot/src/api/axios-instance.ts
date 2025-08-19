import axios, { AxiosRequestConfig, AxiosError } from 'axios';
import type { Context } from 'grammy';

import { ensureUserAccessToken, invalidateUserToken } from '../auth';

const API_BASE_URL = process.env.API_BASE_URL;

export async function photobankAxios<T>(config: AxiosRequestConfig, ctx: Context) {
  async function doRequest(force = false) {
    const token = await ensureUserAccessToken(ctx, force);
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
      invalidateUserToken(ctx);
      return await doRequest(true);
    }
    throw err;
  }
}
