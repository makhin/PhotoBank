export const API_BASE_URL: string =
  import.meta.env.VITE_API_BASE_URL ||
  (typeof process !== 'undefined' ? process.env.API_BASE_URL || '' : '');
export const BOT_TOKEN: string =
  import.meta.env.VITE_BOT_TOKEN ||
  (typeof process !== 'undefined' ? process.env.BOT_TOKEN || '' : '');
export const API_EMAIL: string =
  import.meta.env.VITE_API_EMAIL ||
  (typeof process !== 'undefined' ? process.env.API_EMAIL || '' : '');
export const API_PASSWORD: string =
  import.meta.env.VITE_API_PASSWORD ||
  (typeof process !== 'undefined' ? process.env.API_PASSWORD || '' : '');
export const AZURE_OPENAI_ENDPOINT: string = import.meta.env.VITE_AZURE_OPENAI_ENDPOINT || '';
export const AZURE_OPENAI_KEY: string = import.meta.env.VITE_AZURE_OPENAI_KEY || '';
export const AZURE_OPENAI_DEPLOYMENT: string = import.meta.env.VITE_AZURE_OPENAI_DEPLOYMENT || '';
export const AZURE_OPENAI_API_VERSION: string | undefined = import.meta.env.VITE_AZURE_OPENAI_API_VERSION;
