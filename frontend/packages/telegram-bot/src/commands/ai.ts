import { Context, InlineKeyboard } from 'grammy';
import {
  aiCommandUsageMsg,
  sorryTryToRequestLaterMsg,
  searchPhotosEmptyMsg,
  apiErrorMsg,
  unknownYearLabel,
  prevPageText,
  nextPageText,
} from '@photobank/shared/constants';
import { parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import { findBestPersonId, findBestTagId } from '@photobank/shared/dictionaries';
import type { FilterDto } from '@photobank/shared/generated';
import { PhotosService } from '@photobank/shared/generated';
import { firstNWords, getFilterHash } from '@photobank/shared/index';
import { currentPagePhotos, deletePhotoMessage } from '../photo';
import { captionCache } from './thisday';

export const aiFilters = new Map<string, FilterDto>();

const PAGE_SIZE = 10;

export function parseAiPrompt(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
  if (!match) return null;
  return match[1].trim();
}

export async function sendAiPage(
  ctx: Context,
  hash: string,
  page: number,
  edit = false,
) {
  const filter = aiFilters.get(hash);
  if (!filter) {
    await ctx.reply(sorryTryToRequestLaterMsg);
    return;
  }

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
      ...filter,
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
          const title = photo.name.slice(0, 10) || 'Без названия';
          const peopleCount = photo.persons?.length ?? 0;
          const isAdult = photo.isAdultContent ? '🔞' : '';
          const isRacy = photo.isRacyContent ? '⚠️' : '';

          const metaParts: string[] = [];
          if (peopleCount > 0) metaParts.push(`👥 ${peopleCount} чел.`);
          if (isAdult) metaParts.push(isAdult);
          if (isRacy) metaParts.push(isRacy);
          const metaLine = metaParts.length ? `\n${metaParts.join(' ')}` : '';

          const cap = photo.captions?.join(' ') ?? '';
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

  if (page > 1) keyboard.text(prevPageText, `ai:${page - 1}:${hash}`);
  if (page < totalPages) keyboard.text(nextPageText, `ai:${page + 1}:${hash}`);

  const text = sections.join('\n');

  if (edit) {
    await ctx
      .editMessageText(text, {
        parse_mode: 'HTML',
        reply_markup: keyboard,
      })
      .catch(() =>
        ctx.reply(text, {
          parse_mode: 'HTML',
          reply_markup: keyboard,
        }),
      );
  } else {
    await ctx.reply(text, {
      parse_mode: 'HTML',
      reply_markup: keyboard,
    });
  }
}

export async function aiCommand(ctx: Context) {
  const prompt = parseAiPrompt(ctx.message?.text);
  if (!prompt) {
    await ctx.reply(aiCommandUsageMsg);
    return;
  }
  try {
    const filter = await parseQueryWithOpenAI(prompt);
    const dto: FilterDto = {} as FilterDto;
    const personIds = filter.persons
      .map((name) => findBestPersonId(name))
      .filter((id): id is number => typeof id === 'number');
    if (personIds.length) dto.persons = personIds;

    const tagIds = filter.tags
      .map((name) => findBestTagId(name))
      .filter((id): id is number => typeof id === 'number');
    if (tagIds.length) dto.tags = tagIds;

    if (filter.dateFrom) dto.takenDateFrom = filter.dateFrom.toISOString();
    if (filter.dateTo) dto.takenDateTo = filter.dateTo.toISOString();

    const hash = await getFilterHash(dto);
    aiFilters.set(hash, dto);

    await sendAiPage(ctx, hash, 1);
  } catch (err) {
    console.error(err);
    await ctx.reply(sorryTryToRequestLaterMsg);
  }
}
