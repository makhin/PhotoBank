import { Context, InputFile } from "grammy";
import { getPhotoById } from "@photobank/shared/api/photos";
import { formatPhotoMessage } from "@photobank/shared/utils/formatPhotoMessage";
import { photoNotFoundMsg } from "@photobank/shared/constants";

export const photoMessages = new Map<number, number>();

async function fetchPhoto(id: number) {
    try {
        return await getPhotoById(id);
    } catch {
        return null;
    }
}

export async function sendPhotoById(ctx: Context, id: number) {
    let photo;

    try {
        photo = await getPhotoById(id);
    } catch {
        await ctx.reply(photoNotFoundMsg);
        return;
    }

    const { caption, image } = formatPhotoMessage(photo);

    if (image) {
        const file = new InputFile(image, `${photo.name}.jpg`);
        await ctx.replyWithPhoto(file, {
            caption,
            parse_mode: "HTML",
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

    const { caption, image } = formatPhotoMessage(photo);

    if (!chatId) {
        if (image) {
            const file = new InputFile(image, `${photo.name}.jpg`);
            await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML" });
        } else {
            await ctx.reply(caption, { parse_mode: "HTML" });
        }
        return;
    }

    const existing = photoMessages.get(chatId);
    if (existing) {
        try {
            if (image) {
                const file = new InputFile(image, `${photo.name}.jpg`);
                await ctx.api.editMessageMedia(chatId, existing, {
                    type: "photo",
                    media: file,
                    caption,
                    parse_mode: "HTML",
                });
            } else {
                await ctx.api.editMessageCaption(chatId, existing, {
                    caption,
                    parse_mode: "HTML",
                });
            }
            return;
        } catch {
            photoMessages.delete(chatId);
        }
    }

    if (image) {
        const file = new InputFile(image, `${photo.name}.jpg`);
        const msg = await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML" });
        photoMessages.set(chatId, msg.message_id);
    } else {
        const msg = await ctx.reply(caption, { parse_mode: "HTML" });
        photoMessages.set(chatId, msg.message_id);
    }
}
