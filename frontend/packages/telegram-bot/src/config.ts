// packages/telegram-bot/src/config.ts
import * as dotenv from 'dotenv';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { botTokenNotDefinedError } from '@photobank/shared/constants';

// Node.js ESM does not provide __dirname, recreate it from import.meta.url
const dirName = path.dirname(fileURLToPath(import.meta.url));
// Load environment variables from the project root when running locally.
// dirName points to `packages/telegram-bot/src` during development,
// so climb up to the repository root to locate the `.env` file.
dotenv.config({ path: path.resolve(dirName, '../../../../.env') });

export const BOT_TOKEN: string = process.env.BOT_TOKEN || '';
if (!BOT_TOKEN) throw new Error(botTokenNotDefinedError);

export const API_BASE_URL = process.env.API_BASE_URL;
export const BOT_SERVICE_KEY = process.env.BOT_SERVICE_KEY;

export const AZURE_OPENAI_ENDPOINT: string = process.env.VITE_AZURE_OPENAI_ENDPOINT || '';
export const AZURE_OPENAI_KEY: string = process.env.VITE_AZURE_OPENAI_KEY || '';
export const AZURE_OPENAI_DEPLOYMENT: string = process.env.VITE_AZURE_OPENAI_DEPLOYMENT || '';
export const AZURE_OPENAI_API_VERSION: string = process.env.VITE_AZURE_OPENAI_API_VERSION || '';

