import { Bot, type CommandContext, type HearsContext, type CallbackQueryContext } from "grammy";

import { sendPhotoById, openPhotoInline } from "../photo";
import { withRegistered } from '../registration';
import type { MyContext } from '../i18n';

// Main command
export function registerPhotoRoutes(bot: Bot<MyContext>) {
    // /photo 123
    bot.command("photo", withRegistered(async (ctx: CommandContext<MyContext>) => {
        const parts = ctx.message?.text?.split(" ");
        const id = Number(parts?.[1]);
        if (!id || isNaN(id)) {
            await ctx.reply(ctx.t('photo-usage'));
            return;
        }
        await sendPhotoById(ctx, id);
    }));

    // /photo123
    bot.hears(/^\/photo(\d+)$/, withRegistered(async (ctx: HearsContext<MyContext>) => {
        const id = Number(ctx.match[1]);
        await sendPhotoById(ctx, id);
    }));

    // "photo 123" — if user types without /
    bot.hears(/^photo\s+(\d+)$/, withRegistered(async (ctx: HearsContext<MyContext>) => {
        const id = Number(ctx.match[1]);
        await sendPhotoById(ctx, id);
    }));

    // inline callback button
    bot.callbackQuery(/^photo:(\d+)$/, withRegistered(async (ctx: CallbackQueryContext<MyContext>) => {
        const id = Number(ctx.match[1]);
        await ctx.answerCallbackQuery();
        await openPhotoInline(ctx, id);
    }));

    bot.callbackQuery(/^photo_nav:(\d+)$/, withRegistered(async (ctx: CallbackQueryContext<MyContext>) => {
        const id = Number(ctx.match[1]);
        await ctx.answerCallbackQuery();
        await openPhotoInline(ctx, id);
    }));
}
