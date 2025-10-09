import { env } from '@/env';

export const API_BASE_URL: string = env.VITE_API_BASE_URL || env.API_BASE_URL || 'http://localhost:5066';
export const BOT_TOKEN: string = env.VITE_BOT_TOKEN || env.BOT_TOKEN || '';
export const API_EMAIL: string = env.VITE_API_EMAIL || env.API_EMAIL || '';
export const API_PASSWORD: string = env.VITE_API_PASSWORD || env.API_PASSWORD || '';
export const AZURE_OPENAI_ENDPOINT: string =
  env.VITE_AZURE_OPENAI_ENDPOINT || env.AZURE_OPENAI_ENDPOINT || '';
export const AZURE_OPENAI_KEY: string = env.VITE_AZURE_OPENAI_KEY || env.AZURE_OPENAI_KEY || '';
export const AZURE_OPENAI_DEPLOYMENT: string =
  env.VITE_AZURE_OPENAI_DEPLOYMENT || env.AZURE_OPENAI_DEPLOYMENT || '';
export const AZURE_OPENAI_API_VERSION: string =
  env.VITE_AZURE_OPENAI_API_VERSION || env.AZURE_OPENAI_API_VERSION || '';
