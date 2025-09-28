import { configureAzureOpenAI } from "@photobank/shared/ai/openai";

import { loadDictionaries, setDictionariesUser } from "./dictionaries";
import {
  AZURE_OPENAI_ENDPOINT,
  AZURE_OPENAI_KEY,
  AZURE_OPENAI_DEPLOYMENT,
  AZURE_OPENAI_API_VERSION,
  OPENAI_ENABLED,
} from "./config";
import { bot } from './bot';
import { sendThisDayPage, thisDayCommand } from "./commands/thisday";
import { captionCache } from "./photo";
import { sendSearchPage, searchCommand, decodeSearchCallback } from "./commands/search";
import { aiCommand, sendAiPage } from "./commands/ai";
import { helpCommand } from "./commands/help";
import { subscribeCommand, initSubscriptionScheduler, restoreSubscriptions } from "./commands/subscribe";
import { registerTagsDictionary } from "./commands/tags";
import { registerPersonsDictionary } from "./commands/persons";
import { registerStoragesDictionary } from "./commands/storages";
import { registerPhotoRoutes } from "./commands/photoRouter";
import { profileCommand } from "./commands/profile";
import { uploadCommand } from "./commands/upload";
import { withRegistered } from './registration';
import { logger } from './logger';
import { handleBotError } from './errorHandler';
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

await Promise.all([
  import('./handlers/deeplink'),
  import('./handlers/inline'),
]);

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

if (OPENAI_ENABLED) {
  configureAzureOpenAI({
    endpoint: AZURE_OPENAI_ENDPOINT,
    apiKey: AZURE_OPENAI_KEY,
    deployment: AZURE_OPENAI_DEPLOYMENT,
    apiVersion: AZURE_OPENAI_API_VERSION,
  });
} else {
  logger.warn('OpenAI disabled: missing configuration');
}
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

registerTagsDictionary(bot, withRegistered);
registerPersonsDictionary(bot, withRegistered);
registerStoragesDictionary(bot, withRegistered);

bot.callbackQuery(/^thisday:(\d+)$/, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const page = parseInt(ctx.match[1]!, 10);
  await ctx.answerCallbackQuery();
  await sendThisDayPage(ctx, page, true);
}));

bot.callbackQuery(/^caption:(\d+)$/, withRegistered(async (ctx) => {
  if (!ctx.match || typeof ctx.match === 'string') {
    throw new Error("Callback query match is undefined.");
  }
  const id = parseInt(ctx.match[1]!, 10);
  const caption = captionCache.get(id);
  await ctx.answerCallbackQuery(caption ?? ctx.t('caption-missing'));
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
  const page = parseInt(ctx.match[1]!, 10);
  const hash = ctx.match[2]!;
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
await restoreSubscriptions(bot);
initSubscriptionScheduler(bot);
