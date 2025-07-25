import {Context, InlineKeyboard} from "grammy";
import {searchPhotos} from "@photobank/shared/api/photos";
import {firstNWords} from "@photobank/shared/index";
import {
    apiErrorMsg,
    sorryTryToRequestLaterMsg,
    todaysPhotosEmptyMsg,
    unknownYearLabel,
    prevPageText,
    nextPageText,
} from "@photobank/shared/constants";
import { currentPagePhotos, deletePhotoMessage } from "../photo";

export const captionCache = new Map<number, string>();

const PAGE_SIZE = 10;

function parsePage(text?: string): number {
    if (!text) return 1;
    const match = text.match(/\/thisday\s+(\d+)/);
    return match ? parseInt(match[1], 10) || 1 : 1;
}

export async function handleThisDay(ctx: Context) {
    const page = parsePage(ctx.message?.text);
    await sendThisDayPage(ctx, page);
}

export const thisDayCommand = handleThisDay;

export async function sendThisDayPage(ctx: Context, page: number, edit = false) {
    const skip = (page - 1) * PAGE_SIZE;
    let queryResult;
    try {
        queryResult = await searchPhotos({ thisDay: true, top: PAGE_SIZE, skip });
    } catch (err) {
        console.error(apiErrorMsg, err);
        await ctx.reply(sorryTryToRequestLaterMsg);
        return;
    }

    if (!queryResult.count || !queryResult.photos?.length) {
        const fallback = todaysPhotosEmptyMsg;
        if (edit) {
            await ctx.editMessageText(fallback).catch(() => ctx.reply(fallback));
        } else {
            await ctx.reply(fallback);
        }
        return;
    }

    const totalPages = Math.ceil(queryResult.count / PAGE_SIZE);

    const chatId = ctx.chat?.id;
    if (chatId) {
        const prev = currentPagePhotos.get(chatId);
        if (prev && prev.page !== page) {
            await deletePhotoMessage(ctx);
        }
    }
    const byYear = new Map<number, Map<string, typeof queryResult.photos>>();

    for (const photo of queryResult.photos) {
        const year = photo.takenDate ? new Date(photo.takenDate).getFullYear() : 0;
        if (!byYear.has(year)) byYear.set(year, new Map());
        const yearMap = byYear.get(year)!;
        const storage = photo.storageName;
        const path = photo.relativePath;
        const key = `${storage} / ${path}`;
        if (!yearMap.has(key)) yearMap.set(key, []);
        yearMap.get(key)!.push(photo);
    }

    const sections: string[] = [];
    const keyboard = new InlineKeyboard();
    const photoIds: number[] = [];

    [...byYear.entries()]
        .sort(([a], [b]) => b - a)
        .forEach(([year, folders]) => {
            sections.push(`\n📅 <b>${year || unknownYearLabel}</b>`);
            [...folders.entries()].forEach(([folder, photos]) => {
                sections.push(`📁 ${folder}`);
                photos.forEach(photo => {
                    const title = photo.name.slice(0, 10) || "Без названия";
                    const peopleCount = photo.persons?.length ?? 0;
                    const isAdult = photo.isAdultContent ? "🔞" : "";
                    const isRacy = photo.isRacyContent ? "⚠️" : "";

                    const metaParts: string[] = [];
                    if (peopleCount > 0) metaParts.push(`👥 ${peopleCount} чел.`);
                    if (isAdult) metaParts.push(isAdult);
                    if (isRacy) metaParts.push(isRacy);
                    const metaLine = metaParts.length ? `\n${metaParts.join(" ")}` : "";

                    const caption = photo.captions?.join(" ") ?? "";
                    const index = photoIds.length + 1;
                    sections.push(`[${index}] <b>${title}</b>\n${firstNWords(caption, 5)} ${metaLine}`);
                    photoIds.push(photo.id);
                });
            });
        });

    photoIds.forEach((id, index) => {
        if (index % 5 === 0) keyboard.row();
        keyboard.text(String(index + 1), `photo:${id}`);
    });

    if (chatId) {
        currentPagePhotos.set(chatId, { page, ids: photoIds });
    }

    keyboard.row();

    sections.push(`\n📄 Страница ${page} из ${totalPages}`);

    if (page > 1) keyboard.text(prevPageText, `thisday:${page - 1}`);
    if (page < totalPages) keyboard.text(nextPageText, `thisday:${page + 1}`);

    const text = sections.join("\n");

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
