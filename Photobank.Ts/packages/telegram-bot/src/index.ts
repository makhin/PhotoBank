import { Bot } from "grammy";
import {BOT_TOKEN} from "./config";
import {sendThisDayPage, thisDayCommand} from "./commands/thisday";
import { loadDictionaries } from "@photobank/shared/dictionaries";
import {photoByIdCommand} from "./commands/photoById";

const bot = new Bot(BOT_TOKEN);

await loadDictionaries();

bot.command(
    "start",
    (ctx) => ctx.reply("Добро пожаловать. Запущен и работает!"),
);

bot.command("thisday", thisDayCommand);

bot.callbackQuery(/^thisday:(\d+)$/, async (ctx) => {
    const page = parseInt(ctx.match[1], 10);
    await ctx.answerCallbackQuery();
    await sendThisDayPage(ctx, page);
});

bot.command("photo", photoByIdCommand);

bot.on("message", (ctx) => ctx.reply("Получил другое сообщение!"));

bot.start();