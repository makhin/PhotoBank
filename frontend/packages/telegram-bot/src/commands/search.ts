import { Context, InlineKeyboard } from "grammy";
import { PhotosService } from "@photobank/shared/generated";
import { firstNWords } from "@photobank/shared/index";
import {
  apiErrorMsg,
  sorryTryToRequestLaterMsg,
  searchPhotosEmptyMsg,
  unknownYearLabel,
  prevPageText,
  nextPageText,
  searchCommandUsageMsg,
} from "@photobank/shared/constants";
import { currentPagePhotos, deletePhotoMessage } from "../photo";
import { captionCache } from "./thisday";

const PAGE_SIZE = 10;

function parseCaption(text?: string): string {
  if (!text) return "";
  const match = text.match(/^\/search\s+(.+)/);
  let caption = match ? match[1].trim() : "";
  if (
    (caption.startsWith('"') && caption.endsWith('"')) ||
    (caption.startsWith("'") && caption.endsWith("'"))
  ) {
    caption = caption.slice(1, -1);
  }
  return caption;
}

export async function handleSearch(ctx: Context) {
  const caption = parseCaption(ctx.message?.text);
  if (!caption) {
    await ctx.reply(searchCommandUsageMsg);
    return;
  }
  await sendSearchPage(ctx, caption, 1);
}

export const searchCommand = handleSearch;

export async function sendSearchPage(
  ctx: Context,
  caption: string,
  page: number,
  edit = false,
) {
  const chatId = ctx.chat?.id;
  if (chatId) {
    const prev = currentPagePhotos.get(chatId);
    if (prev && prev.page !== page) {
      await deletePhotoMessage(ctx);
    }
  }

  const skip = (page - 1) * PAGE_SIZE;
  let queryResult;
  try {
    queryResult = await PhotosService.postApiPhotosSearch({
      caption,
      top: PAGE_SIZE,
      skip,
    });
  } catch (err) {
    console.error(apiErrorMsg, err);
    await ctx.reply(sorryTryToRequestLaterMsg);
    return;
  }

  if (!queryResult.count || !queryResult.photos?.length) {
    const fallback = searchPhotosEmptyMsg;
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
        photos.forEach((photo) => {
          const title = photo.name.slice(0, 10) || "Без названия";
          const peopleCount = photo.persons?.length ?? 0;
          const isAdult = photo.isAdultContent ? "🔞" : "";
          const isRacy = photo.isRacyContent ? "⚠️" : "";

          const metaParts: string[] = [];
          if (peopleCount > 0) metaParts.push(`👥 ${peopleCount} чел.`);
          if (isAdult) metaParts.push(isAdult);
          if (isRacy) metaParts.push(isRacy);
          const metaLine = metaParts.length ? `\n${metaParts.join(" ")}` : "";

          const cap = photo.captions?.join(" ") ?? "";
          const index = photoIds.length + 1;
          sections.push(`[${index}] <b>${title}</b>\n${firstNWords(cap, 5)} ${metaLine}`);
          captionCache.set(photo.id, cap);
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

  if (page > 1) keyboard.text(prevPageText, `search:${page - 1}:${encodeURIComponent(caption)}`);
  if (page < totalPages) keyboard.text(nextPageText, `search:${page + 1}:${encodeURIComponent(caption)}`);

  const text = sections.join("\n");

  if (edit) {
    await ctx
      .editMessageText(text, {
        parse_mode: "HTML",
        reply_markup: keyboard,
      })
      .catch(() =>
        ctx.reply(text, {
          parse_mode: "HTML",
          reply_markup: keyboard,
        }),
      );
  } else {
    await ctx.reply(text, {
      parse_mode: "HTML",
      reply_markup: keyboard,
    });
  }
}
