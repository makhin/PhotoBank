import { Context, InlineKeyboard } from "grammy";
import { searchPhotos } from "@photobank/shared/api/photos";

const PAGE_SIZE = 10;

function parsePage(text?: string): number {
    if (!text) return 1;
    const match = text.match(/\/thisday\s+(\d+)/);
    return match ? parseInt(match[1], 10) || 1 : 1;
}

export async function thisDayCommand(ctx: Context) {
    const page = parsePage(ctx.message?.text);
    await sendThisDayPage(ctx, page);
}

export async function sendThisDayPage(ctx: Context, page: number) {
    const skip = (page - 1) * PAGE_SIZE;

    const queryResult = await searchPhotos({
        thisDay: true,
        top: PAGE_SIZE,
        skip,
    });

    if (!queryResult.count || !queryResult.photos?.length) {
        await ctx.reply("📭 Сегодняшних фото пока нет.");
        return;
    }

    const totalPages = Math.ceil(queryResult.count / PAGE_SIZE);
    const lines = queryResult.photos.map(photo => {
        const year = photo.takenDate ? new Date(photo.takenDate).getFullYear() : "?";
        return `📸 ${photo.name ?? "Без названия"}\n🗓 ${year}\n📁 ${photo.storageName} / ${photo.relativePath}`;
    });

    const keyboard = new InlineKeyboard();
    if (page > 1) keyboard.text("◀ Назад", `thisday:${page - 1}`);
    if (page < totalPages) keyboard.text("Вперёд ▶", `thisday:${page + 1}`);

    await ctx.reply(lines.join("\n\n") + `\n\n📄 Страница ${page} из ${totalPages}`, {
        reply_markup: keyboard,
    });
}
