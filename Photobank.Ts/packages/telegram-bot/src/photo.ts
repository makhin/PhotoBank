import { Context, InputFile } from "grammy";
import { getPhotoById } from "@photobank/shared/api/photos";
import { formatPhotoMessage } from "@photobank/shared/utils/formatPhotoMessage";

export async function sendPhotoById(ctx: Context, id: number) {
    let photo;

    try {
        photo = await getPhotoById(id);
    } catch {
        await ctx.reply("❌ Фото не найдено.");
        return;
    }

    const { caption, image } = formatPhotoMessage(photo);

    if (image) {
        const file = new InputFile(image, `${photo.name ?? "photo"}.jpg`);
        await ctx.replyWithPhoto(file, {
            caption,
            parse_mode: "HTML",
        });
    } else {
        await ctx.reply(caption, { parse_mode: "HTML" });
    }
}
