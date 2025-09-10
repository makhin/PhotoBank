import { Bot } from 'grammy';

import { BOT_TOKEN } from './config.js';
import type { MyContext } from './i18n.js';

export const bot = new Bot<MyContext>(BOT_TOKEN);
