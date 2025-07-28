// packages/telegram-bot/src/config.ts
import * as dotenv from 'dotenv';
import path from 'node:path';
import {
  apiCredentialsNotDefinedError,
  botTokenNotDefinedError,
} from '@photobank/shared/constants';
dotenv.config({ path: path.resolve(__dirname, '../../.env') });

export const BOT_TOKEN: string = process.env.BOT_TOKEN || '';
if (!BOT_TOKEN) throw new Error(botTokenNotDefinedError);

export const API_EMAIL: string = process.env.API_EMAIL || '';
export const API_PASSWORD: string = process.env.API_PASSWORD || '';
if (!API_EMAIL || !API_PASSWORD) {
  throw new Error(apiCredentialsNotDefinedError);
}

export const API_BASE_URL: string =
  process.env.VITE_API_BASE_URL || process.env.API_BASE_URL || 'http://localhost:5066';

export const AZURE_OPENAI_ENDPOINT: string = process.env.VITE_AZURE_OPENAI_ENDPOINT || '';
export const AZURE_OPENAI_KEY: string = process.env.VITE_AZURE_OPENAI_KEY || '';
export const AZURE_OPENAI_DEPLOYMENT: string = process.env.VITE_AZURE_OPENAI_DEPLOYMENT || '';
export const AZURE_OPENAI_API_VERSION: string | undefined = process.env.VITE_AZURE_OPENAI_API_VERSION;

