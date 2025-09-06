import { configureAzureOpenAI } from "@photobank/shared/ai/openai";

import { loadDictionaries, setDictionariesUser } from "./dictionaries.js";
import {
  AZURE_OPENAI_ENDPOINT,
  AZURE_OPENAI_KEY,
  AZURE_OPENAI_DEPLOYMENT,
  AZURE_OPENAI_API_VERSION,
} from "./config.js";
import { bot } from './bot.js';
import { sendThisDayPage, thisDayCommand } from "./commands/thisday.js";
import { captionCache } from "./photo.js";
import { sendSearchPage, searchCommand, decodeSearchCallback } from "./commands/search.js";
import { aiCommand, sendAiPage } from "./commands/ai.js";
import { helpCommand } from "./commands/help.js";
import { subscribeCommand, initSubscriptionScheduler } from "./commands/subscribe.js";
import { tagsCommand, sendTagsPage } from "./commands/tags.js";
import { personsCommand, sendPersonsPage } from "./commands/persons.js";
import { storagesCommand, sendStoragesPage } from "./commands/storages.js";
import { tagsCallbackPattern, personsCallbackPattern, storagesCallbackPattern } from "./patterns.js";
import { registerPhotoRoutes } from "./commands/photoRouter.js";
import { profileCommand } from "./commands/profile.js";
import { uploadCommand } from "./commands/upload.js";
import { withRegistered } from './registration.js';
import { logger } from './logger.js';
import { handleBotError } from './errorHandler.js';
import './handlers/inline.js';
import './handlers/deeplink.js';
import { i18n } from './i18n.js';

const privateCommands = (lang: string) => [
  { command: 'start', description: i18n.t(lang, 'cmd-start') },
  { command: 'help', description: i18n.t(lang, 'cmd-help') },
  { command: 'thisday', description: i18n.t(lang, 'cmd-thisday') },
  { command: 'search', description: i18n.t(lang, 'cmd-search') },
  { command: 'ai', description: i18n.t(lang, 'cmd-ai') },
  { command: 'profile', description: i18n.t(lang, 'cmd-profile') },
  { command: 'subscribe', description: i18n.t(lang, 'cmd-subscribe') },
  { command: 'tags', description: i18n.t(lang, 'cmd-tags') },
  { command: 'persons', description: i18n.t(lang, 'cmd-persons') },
  { command: 'storages', description: i18n.t(lang, 'cmd-storages') },
  { command: 'upload', description: i18n.t(lang, 'cmd-upload') },
];

const groupCommands = (lang: string) => [
  { command: 'help', description: i18n.t(lang, 'cmd-help') },
  { command: 'thisday', description: i18n.t(lang, 'cmd-thisday') },
];

bot.use(i18n.middleware());
bot.use(async (ctx, next) => {
  const lang = ctx.from?.language_code?.split('-')[0];
  if (lang) ctx.i18n.useLocale(lang);
  await next();
});

bot.use(async (ctx, next) => {
  const username = ctx.from?.username ?? String(ctx.from?.id ?? '');
  const updateType = Object.keys(ctx.update).find(k => k !== 'update_id') ?? 'unknown';
  logger.info(`update from ${username}: ${updateType}`);
  await next();
});

bot.use(async (ctx, next) => {
  const locale = await ctx.i18n.getLocale();
  setDictionariesUser(ctx.from?.id, locale);
  await loadDictionaries(ctx);
  await next();
});

bot.catch(handleBotError);

registerPhotoRoutes(bot);


configureAzureOpenAI({
  endpoint: AZURE_OPENAI_ENDPOINT,
  apiKey: AZURE_OPENAI_KEY,
  deployment: AZURE_OPENAI_DEPLOYMENT,
  apiVersion: AZURE_OPENAI_API_VERSION,
});
bot.command(
  "start",
  (ctx) => ctx.reply(ctx.t('welcome')),
);
bot.command("help", helpCommand);

bot.command("thisday", withRegistered(thisDayCommand));
bot.command("search", withRegistered(searchCommand));
bot.command("ai", withRegistered(aiCommand));

bot.command("profile", profileCommand);

bot.command("subscribe", withRegistered(subscribeCommand));
bot.command("upload", withRegistered(uploadCommand));

bot.command("tags", withRegistered(tagsCommand));
bot.command("persons", withRegistered(personsCommand));
bot.command("storages", withRegistered(storagesCommand));

bot.callbackQuery(/^thisday:(\d+)$/, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  await ctx.answerCallbackQuery();
  await sendThisDayPage(ctx, page, true);
}));

bot.callbackQuery(/^caption:(\d+)$/, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const id = parseInt(ctx.match[1], 10);
  const caption = captionCache.get(id);
  await ctx.answerCallbackQuery(caption ?? ctx.t('caption-missing'));
}));

bot.callbackQuery(tagsCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendTagsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(personsCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendPersonsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(storagesCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendStoragesPage(ctx, prefix, page, true);
}));

bot.callbackQuery(/^search:(\d+):(.+)$/, withRegistered(async (ctx) => {
  const data = ctx.callbackQuery?.data;
  if (!data) {
    throw new Error("Callback query data is undefined.");
  }
  const decoded = decodeSearchCallback(data);
  if (!decoded) {
    await ctx.answerCallbackQuery();
    return;
  }
  await ctx.answerCallbackQuery();
  await sendSearchPage(ctx, decoded.filter, decoded.page, true);
}));

bot.callbackQuery(/^ai:(\d+):([\w-]+)$/, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const hash = ctx.match[2];
  await ctx.answerCallbackQuery();
  await sendAiPage(ctx, hash, page, true);
}));

bot.on('message:text', withRegistered(async (ctx) => {
  const text = ctx.message?.text;
  if (!text || text.startsWith('/')) return;
  await aiCommand(ctx, text);
}));

bot.on('message:photo', withRegistered(uploadCommand));
bot.on('message:document', withRegistered(uploadCommand));

await bot.api.setMyCommands(privateCommands('en'), {
  scope: { type: 'all_private_chats' },
  language_code: 'en',
});
await bot.api.setMyCommands(privateCommands('ru'), {
  scope: { type: 'all_private_chats' },
  language_code: 'ru',
});
await bot.api.setMyCommands(groupCommands('en'), {
  scope: { type: 'all_group_chats' },
  language_code: 'en',
});
await bot.api.setMyCommands(groupCommands('ru'), {
  scope: { type: 'all_group_chats' },
  language_code: 'ru',
});

bot.start();
logger.info('bot started');
initSubscriptionScheduler(bot);
