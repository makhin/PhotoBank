import { Bot } from "grammy";
import {BOT_TOKEN, API_EMAIL, API_PASSWORD} from "./config";
import {sendThisDayPage, thisDayCommand, captionCache} from "./commands/thisday";
import { subscribeCommand, initSubscriptionScheduler } from "./commands/subscribe";
import { loadDictionaries } from "@photobank/shared/dictionaries";
import { photoByIdCommand } from "./commands/photoById";
import { registerPhotoRoutes } from "./commands/photoRouter";
import { profileCommand } from "./commands/profile";
import { login, setImpersonateUser, setApiBaseUrl } from "@photobank/shared/api";
import { loadResources, getApiBaseUrl } from "@photobank/shared/config";
import {
    captionMissingMsg,
    unknownMessageReplyMsg,
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

bot.command(
    "start",
    (ctx) => ctx.reply(welcomeBotMsg),
);

bot.command("thisday", thisDayCommand);

bot.command("photo", photoByIdCommand);

bot.command("profile", profileCommand);

bot.command("subscribe", subscribeCommand);

bot.callbackQuery(/^thisday:(\d+)$/, async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    await ctx.answerCallbackQuery();
    await sendThisDayPage(ctx, page, true);
});

bot.callbackQuery(/^caption:(\d+)$/, async (ctx) => {
    const id = parseInt(ctx.match[1], 10);
    const caption = captionCache.get(id);
    await ctx.answerCallbackQuery(caption ?? captionMissingMsg, { show_alert: true });
});

bot.on("message", (ctx) => ctx.reply(unknownMessageReplyMsg));

bot.start();
initSubscriptionScheduler(bot);
