import {Context, InputFile} from "grammy";
import { searchPhotos } from "@photobank/shared/api/photos";

export async function thisDayCommand(ctx: Context) {
    const queryResult = await searchPhotos({thisDay: true, top: 10});

    if (!queryResult.count) {
        await ctx.reply("📭 Сегодняшних фото пока нет.");
        return;
    }

    if (queryResult.photos) {
    for (const photo of queryResult.photos) {
        if (!photo.thumbnail) continue;

        const buffer = Buffer.from(photo.thumbnail, "base64");
        const file = new InputFile(buffer);
        await ctx.replyWithPhoto(file, {
            caption: `📸 ${photo.name ?? "Без названия"}\n🗓 ${photo.takenDate?.slice(0, 10) ?? "?"}`
        });
    }
    }
}
