import { Context, InlineKeyboard } from "grammy";
import { searchPhotos } from "@photobank/shared/api/photos";

export const captionCache = new Map<number, string>();

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

export async function sendThisDayPage(ctx: Context, page: number, edit = false) {
    const skip = (page - 1) * PAGE_SIZE;
    const queryResult = await searchPhotos({ thisDay: true, top: PAGE_SIZE, skip });

    if (!queryResult.count || !queryResult.photos?.length) {
        const fallback = "📭 Сегодняшних фото пока нет.";
        if (edit) {
            await ctx.editMessageText(fallback).catch(() => ctx.reply(fallback));
        } else {
            await ctx.reply(fallback);
        }
        return;
    }

    const totalPages = Math.ceil(queryResult.count / PAGE_SIZE);
    const byYear = new Map<number, Map<string, typeof queryResult.photos>>();

    for (const photo of queryResult.photos) {
        const year = photo.takenDate ? new Date(photo.takenDate).getFullYear() : 0;
        if (!byYear.has(year)) byYear.set(year, new Map());
        const yearMap = byYear.get(year)!;
        const storage = photo.storageName ?? "???";
        const path = photo.relativePath ?? "-";
        const key = `${storage} / ${path}`;
        if (!yearMap.has(key)) yearMap.set(key, []);
        yearMap.get(key)!.push(photo);
    }

    const sections: string[] = [];
    const keyboard = new InlineKeyboard();

    [...byYear.entries()]
        .sort(([a], [b]) => b - a)
        .forEach(([year, folders]) => {
            sections.push(`📅 <b>${year || "Неизвестный год"}</b>`);
            [...folders.entries()].forEach(([folder, photos]) => {
                sections.push(`📁 ${folder}`);
                photos.forEach(photo => {
                    const title = photo.name ?? `Фото ${photo.id}`;
                    const peopleCount = photo.persons?.length ?? 0;
                    const isAdult = photo.isAdultContent ? "🔞" : "";
                    const isRacy = photo.isRacyContent ? "⚠️" : "";

                    const metaParts: string[] = [];
                    if (peopleCount > 0) metaParts.push(`👥 ${peopleCount} чел.`);
                    if (isAdult) metaParts.push(isAdult);
                    if (isRacy) metaParts.push(isRacy);

                    const caption = photo.captions?.join(" ") ?? "";

                    const metaLine = metaParts.length ? `\n${metaParts.join(" ")}` : "";
                    sections.push(`• <b>${title}</b>${metaLine} 🔗 /photo${photo.id}`);
                    if (caption) {
                        captionCache.set(photo.id, caption);
                        keyboard.text("ℹ️", `caption:${photo.id}`).row();
                    }
                });
            });
        });

    sections.push(`\n📄 Страница ${page} из ${totalPages}`);
    keyboard.row();

    if (page > 1) keyboard.text("◀ Назад", `thisday:${page - 1}`);
    if (page < totalPages) keyboard.text("Вперёд ▶", `thisday:${page + 1}`);

    const text = sections.join("\n\n");

    if (edit) {
        await ctx.editMessageText(text, {
            parse_mode: "HTML",
            reply_markup: keyboard,
        }).catch(() => ctx.reply(text, {
            parse_mode: "HTML",
            reply_markup: keyboard,
        }));
    } else {
        await ctx.reply(text, {
            parse_mode: "HTML",
            reply_markup: keyboard,
        });
    }
}
