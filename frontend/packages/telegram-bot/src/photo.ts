import { Context, InputFile, InlineKeyboard } from "grammy";
import { PhotosService } from "@photobank/shared/generated";
import { formatPhotoMessage } from "@photobank/shared/utils/formatPhotoMessage";
import { photoNotFoundMsg, prevPageText, nextPageText } from "@photobank/shared/constants";

export const photoMessages = new Map<number, number>();
export const currentPagePhotos = new Map<number, { page: number; ids: number[] }>();

export async function deletePhotoMessage(ctx: Context) {
    const chatId = ctx.chat?.id;
    const messageId = chatId ? photoMessages.get(chatId) : undefined;
    if (chatId && messageId) {
        try {
            await ctx.api.deleteMessage(chatId, messageId);
        } catch {
            // ignore
        }
        photoMessages.delete(chatId);
    }
}

async function fetchPhoto(id: number) {
    try {
        return await PhotosService.getApiPhotos(id);
    } catch {
        return null;
    }
}

export async function sendPhotoById(ctx: Context, id: number) {
    let photo;

    try {
        photo = await PhotosService.getApiPhotos(id);
    } catch {
        await ctx.reply(photoNotFoundMsg);
        return;
    }

    const { caption, hasSpoiler, image } = formatPhotoMessage(photo);

    if (image) {
        const file = new InputFile(image, `${photo.name}.jpg`);
        await ctx.replyWithPhoto(file, {
            caption,
            parse_mode: "HTML",
            has_spoiler: hasSpoiler,
        });
    } else {
        await ctx.reply(caption, { parse_mode: "HTML" });
    }
}

export async function openPhotoInline(ctx: Context, id: number) {
    const chatId = ctx.chat?.id;
    const photo = await fetchPhoto(id);
    if (!photo) {
        await ctx.reply(photoNotFoundMsg);
        return;
    }

    const { caption, hasSpoiler, image } = formatPhotoMessage(photo);

    let keyboard: InlineKeyboard | undefined;
    if (chatId) {
        const list = currentPagePhotos.get(chatId);
        if (list) {
            const index = list.ids.indexOf(id);
            keyboard = new InlineKeyboard();
            if (index > 0) keyboard.text(prevPageText, `photo_nav:${list.ids[index - 1]}`);
            if (index < list.ids.length - 1) keyboard.text(nextPageText, `photo_nav:${list.ids[index + 1]}`);
            if (!keyboard.inline_keyboard.length) keyboard = undefined;
        }
    }

    if (!chatId) {
        if (image) {
            const file = new InputFile(image, `${photo.name}.jpg`);
            await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML", reply_markup: keyboard, has_spoiler: hasSpoiler });
        } else {
            await ctx.reply(caption, { parse_mode: "HTML", reply_markup: keyboard });
        }
        return;
    }

    const existing = photoMessages.get(chatId);
    if (existing) {
        try {
            if (image) {
                const file = new InputFile(image, `${photo.name}.jpg`);
                await ctx.api.editMessageMedia(
                    chatId,
                    existing,
                    {
                        type: "photo",
                        media: file,
                        caption,
                        parse_mode: "HTML",
                    },
                    { reply_markup: keyboard }
                );
            } else {
                await ctx.api.editMessageCaption(chatId, existing, { caption, parse_mode: "HTML" });
            }
            return;
        } catch {
            photoMessages.delete(chatId);
        }
    }

    if (image) {
        const file = new InputFile(image, `${photo.name}.jpg`);
        const msg = await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML", reply_markup: keyboard, has_spoiler: hasSpoiler });
        photoMessages.set(chatId, msg.message_id);
    } else {
        const msg = await ctx.reply(caption, { parse_mode: "HTML", reply_markup: keyboard });
        photoMessages.set(chatId, msg.message_id);
    }
}
