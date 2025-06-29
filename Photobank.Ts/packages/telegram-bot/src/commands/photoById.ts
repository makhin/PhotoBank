import { Context, InputFile } from "grammy";
import { getPhotoById } from "@photobank/shared/api";
import { formatPhotoMessage } from "@photobank/shared/utils/formatPhotoMessage";

export async function photoByIdCommand(ctx: Context) {
    const parts = ctx.message?.text?.split(" ");
    const id = Number(parts?.[1]);

    if (!id || isNaN(id)) {
        await ctx.reply("❗ Используй: /photo <id>");
        return;
    }

    try {
        const photo = await getPhotoById(id);

        if (!photo) {
            await ctx.reply("❌ Фото не найдено.");
            return;
        }

        const { caption, image } = formatPhotoMessage(photo);

        if (image) {
            const file = new InputFile(image, `${photo.name ?? "photo"}.jpg`);
            await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML" });
        } else {
            await ctx.reply(caption, { parse_mode: "HTML" });
        }
    } catch (error) {
        console.error("Ошибка при получении фото:", error);
        await ctx.reply("🚫 Не удалось получить фото.");
    }
}
