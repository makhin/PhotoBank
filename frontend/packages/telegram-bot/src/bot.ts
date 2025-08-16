import { Bot } from 'grammy';

import { BOT_TOKEN } from './config';
import type { MyContext } from './i18n';

export const bot = new Bot<MyContext>(BOT_TOKEN);
