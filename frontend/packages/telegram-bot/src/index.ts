import {Bot} from "grammy";
import {
    BOT_TOKEN,
    API_EMAIL,
    API_PASSWORD,
    AZURE_OPENAI_ENDPOINT,
    AZURE_OPENAI_KEY,
    AZURE_OPENAI_DEPLOYMENT,
    AZURE_OPENAI_API_VERSION,
} from "./config";
import {sendThisDayPage, thisDayCommand, captionCache} from "./commands/thisday";
import { sendSearchPage, searchCommand } from "./commands/search";
import { aiCommand } from "./commands/ai";
import { subscribeCommand, initSubscriptionScheduler } from "./commands/subscribe";
import { tagsCommand, sendTagsPage } from "./commands/tags";
import { personsCommand, sendPersonsPage } from "./commands/persons";
import { tagsCallbackPattern, personsCallbackPattern } from "./patterns";
import { loadDictionaries } from "@photobank/shared/dictionaries";
import { registerPhotoRoutes } from "./commands/photoRouter";
import { profileCommand } from "./commands/profile";
import { withRegistered } from './registration';
import {
    login,
    setImpersonateUser,
    setApiBaseUrl,
    configureAzureOpenAI,
} from "@photobank/shared/api";
import { loadResources, getApiBaseUrl } from "@photobank/shared/config";
import {
    captionMissingMsg,
    welcomeBotMsg,
} from "@photobank/shared/constants";

const bot = new Bot(BOT_TOKEN);

bot.use(async (ctx, next) => {
    const username = ctx.from?.username ?? String(ctx.from?.id ?? '');
    setImpersonateUser(username);
    await next();
});

registerPhotoRoutes(bot);

await loadResources();
setApiBaseUrl(getApiBaseUrl());

await login({ email: API_EMAIL, password: API_PASSWORD });
await loadDictionaries();

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

bot.command("thisday", withRegistered(thisDayCommand));
bot.command("search", withRegistered(searchCommand));
bot.command("ai", aiCommand);

bot.command("profile", profileCommand);

bot.command("subscribe", withRegistered(subscribeCommand));

bot.command("tags", withRegistered(tagsCommand));
bot.command("persons", withRegistered(personsCommand));

bot.callbackQuery(/^thisday:(\d+)$/, withRegistered(async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    await ctx.answerCallbackQuery();
    await sendThisDayPage(ctx, page, true);
}));

bot.callbackQuery(/^caption:(\d+)$/, withRegistered(async (ctx) => {
    const id = parseInt(ctx.match[1], 10);
    const caption = captionCache.get(id);
    await ctx.answerCallbackQuery(caption ?? captionMissingMsg);
}));

bot.callbackQuery(tagsCallbackPattern, withRegistered(async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    const prefix = decodeURIComponent(ctx.match[2]);
    await ctx.answerCallbackQuery();
    await sendTagsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(personsCallbackPattern, withRegistered(async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    const prefix = decodeURIComponent(ctx.match[2]);
    await ctx.answerCallbackQuery();
    await sendPersonsPage(ctx, prefix, page, true);
}));

bot.callbackQuery(/^search:(\d+):(.+)$/, withRegistered(async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    const caption = decodeURIComponent(ctx.match[2]);
    await ctx.answerCallbackQuery();
    await sendSearchPage(ctx, caption, page, true);
}));

bot.start();
initSubscriptionScheduler(bot);
