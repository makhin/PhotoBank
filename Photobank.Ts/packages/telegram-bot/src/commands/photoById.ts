import {Context, InputFile} from "grammy";
import { getPhotoById } from "@photobank/shared/api";
import {getPersonName, getStorageName} from "@photobank/shared/dictionaries";
import {formatDate} from "@photobank/shared/index";

export async function photoByIdCommand(ctx: Context) {
    const parts = ctx.message?.text?.split(" ");
    const id = Number(parts?.[1]);

    if (!id || isNaN(id)) {
        await ctx.reply("❗ Используй: /photo <id>");
        return;
    }

    try {
        const photo = await getPhotoById(id);

        if (!photo || !photo.previewImage) {
            await ctx.reply("❌ Фото не найдено или нет превью.");
            return;
        }

        const buffer = Buffer.from(photo.previewImage, "base64");
        const file = new InputFile(buffer);

        const tags = photo.tags?.length ? `🏷️ ${photo.tags.join(", ")}` : "";
        const people = photo.faces
            ?.map(f => f.personId ?? 0)
            .filter(Boolean)
            .map(getPersonName)
            .join(", ");
        const takenDate = formatDate(photo.takenDate);

        const persons = people ? `👤 ${people}` : "";
        const captionLines = [
            `📸 ${photo.name}`,
            `📅 ${takenDate}`,
            `📝 ${(photo.captions ? photo.captions[0] : "Нет описания")}`,
            persons,
            tags,
        ].filter(Boolean);

        await ctx.replyWithPhoto(file, {
            caption: captionLines.join("\n")
        });
    } catch (error) {
        console.error("Ошибка при получении фото:", error);
        await ctx.reply("🚫 Не удалось получить фото.");
    }
}
