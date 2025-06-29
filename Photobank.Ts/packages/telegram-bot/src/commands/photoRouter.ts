import { Bot, Context } from "grammy";
import {sendPhotoById} from "../photo";

// Основная команда
export function registerPhotoRoutes(bot: Bot) {
    // /photo 123
    bot.command("photo", async (ctx) => {
        const parts = ctx.message?.text?.split(" ");
        const id = Number(parts?.[1]);
        if (!id || isNaN(id)) {
            await ctx.reply("❗ Используй: /photo <id>");
            return;
        }
        await sendPhotoById(ctx, id);
    });

    // /photo123
    bot.hears(/^\/photo(\d+)$/, async (ctx) => {
        const id = Number(ctx.match[1]);
        await sendPhotoById(ctx, id);
    });

    // "photo 123" — если вдруг пользователь пишет без /
    bot.hears(/^photo\s+(\d+)$/, async (ctx) => {
        const id = Number(ctx.match[1]);
        await sendPhotoById(ctx, id);
    });

    // inline callback-кнопка
    bot.callbackQuery(/^photo:(\d+)$/, async (ctx) => {
        const id = Number(ctx.match[1]);
        await ctx.answerCallbackQuery();
        await sendPhotoById(ctx, id);
    });
}
