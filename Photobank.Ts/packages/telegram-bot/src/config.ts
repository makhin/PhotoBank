// packages/telegram-bot/src/config.ts
import * as dotenv from 'dotenv';
dotenv.config();

export const BOT_TOKEN: string = process.env.BOT_TOKEN || '';
if (!BOT_TOKEN) throw new Error('BOT_TOKEN is not defined');
