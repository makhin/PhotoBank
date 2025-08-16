import { configureAzureOpenAI } from "@photobank/shared/ai/openai";
import {
  captionMissingMsg,
  welcomeBotMsg,
} from "@photobank/shared/constants";

import { loadDictionaries, setDictionariesUser } from "./dictionaries";
import {
  AZURE_OPENAI_ENDPOINT,
  AZURE_OPENAI_KEY,
  AZURE_OPENAI_DEPLOYMENT,
  AZURE_OPENAI_API_VERSION,
} from "./config";
import { bot } from './bot';
import { sendThisDayPage, thisDayCommand } from "./commands/thisday";
import { captionCache } from "./photo";
import { sendSearchPage, searchCommand } from "./commands/search";
import { aiCommand, sendAiPage } from "./commands/ai";
import { helpCommand } from "./commands/help";
import { subscribeCommand, initSubscriptionScheduler } from "./commands/subscribe";
import { tagsCommand, sendTagsPage } from "./commands/tags";
import { personsCommand, sendPersonsPage } from "./commands/persons";
import { storagesCommand, sendStoragesPage } from "./commands/storages";
import { tagsCallbackPattern, personsCallbackPattern, storagesCallbackPattern } from "./patterns";
import { registerPhotoRoutes } from "./commands/photoRouter";
import { profileCommand } from "./commands/profile";
import { uploadCommand } from "./commands/upload";
import { withRegistered } from './registration';
import { logger } from './logger';
import { handleBotError } from './errorHandler';
import './handlers/inline';
import './handlers/deeplink';
import { i18n } from './i18n';

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
  setDictionariesUser(ctx.from?.id);
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
  (ctx) => ctx.reply(welcomeBotMsg),
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
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  await ctx.answerCallbackQuery();
  await sendThisDayPage(ctx, page, true);
}));

bot.callbackQuery(/^caption:(\d+)$/, withRegistered(async (ctx) => {
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const id = parseInt(ctx.match[1], 10);
  const caption = captionCache.get(id);
  await ctx.answerCallbackQuery(caption ?? captionMissingMsg);
}));

bot.callbackQuery(tagsCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendTagsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(personsCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendPersonsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(storagesCallbackPattern, withRegistered(async (ctx) => {
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const prefix = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendStoragesPage(ctx, prefix, page, true);
}));

bot.callbackQuery(/^search:(\d+):(.+)$/, withRegistered(async (ctx) => {
  if (!ctx.match) {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1], 10);
  const caption = decodeURIComponent(ctx.match[2]);
  await ctx.answerCallbackQuery();
  await sendSearchPage(ctx, caption, page, true);
}));

bot.callbackQuery(/^ai:(\d+):([\w-]+)$/, withRegistered(async (ctx) => {
  if (!ctx.match) {
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
