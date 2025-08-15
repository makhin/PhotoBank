import axios, { AxiosRequestConfig } from 'axios';
import type { Context } from 'grammy';
import { ensureUserAccessToken } from '../auth';

const API_BASE_URL = process.env.API_BASE_URL!;

export async function photobankAxios<T>(config: AxiosRequestConfig, ctx: Context) {
  const token = await ensureUserAccessToken(ctx);
  return axios<T>({
    baseURL: API_BASE_URL,
    headers: { Authorization: `Bearer ${token}`, ...(config.headers || {}) },
    ...config,
  });
}
