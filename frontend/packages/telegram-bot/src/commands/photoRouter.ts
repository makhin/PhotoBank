import { Bot, Context } from "grammy";
import { sendPhotoById, openPhotoInline } from "../photo";
import { photoCommandUsageMsg } from "@photobank/shared/constants";

// Основная команда
export function registerPhotoRoutes(bot: Bot) {
    // /photo 123
    bot.command("photo", async (ctx) => {
        const parts = ctx.message?.text?.split(" ");
        const id = Number(parts?.[1]);
        if (!id || isNaN(id)) {
            await ctx.reply(photoCommandUsageMsg);
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
        await openPhotoInline(ctx, id);
    });

    bot.callbackQuery(/^photo_nav:(\d+)$/, async (ctx) => {
        const id = Number(ctx.match[1]);
        await ctx.answerCallbackQuery();
        await openPhotoInline(ctx, id);
    });
}
