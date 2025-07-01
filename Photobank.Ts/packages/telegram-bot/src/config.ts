// packages/telegram-bot/src/config.ts
import * as dotenv from 'dotenv';
dotenv.config();

export const BOT_TOKEN: string = process.env.BOT_TOKEN || '';
if (!BOT_TOKEN) throw new Error('BOT_TOKEN is not defined');

export const API_EMAIL: string = process.env.API_EMAIL || '';
export const API_PASSWORD: string = process.env.API_PASSWORD || '';
if (!API_EMAIL || !API_PASSWORD) {
  throw new Error('API_EMAIL or API_PASSWORD is not defined');
}

