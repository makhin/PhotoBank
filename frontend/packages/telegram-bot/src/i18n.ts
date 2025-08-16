import { I18n } from '@grammyjs/i18n';
import type { Context } from 'grammy';
import type { I18nFlavor } from '@grammyjs/i18n';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));

export const i18n = new I18n({
  defaultLocale: 'en',
  directory: resolve(__dirname, 'locales'),
});

export type MyContext = Context & I18nFlavor;
