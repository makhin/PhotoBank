import { InlineKeyboard } from 'grammy';
import { getYear, isValid, parseISO } from 'date-fns';
import {
  firstNWords,
  type FilterDto,
  withPagination,
} from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import type { MyContext } from '../i18n';
import { searchPhotos } from '../services/photo';
import { handleCommandError } from '../errorHandler';
import { captionCache, currentPagePhotos, deletePhotoMessage } from '../photo';
import { setLastFilter } from '../cache/lastFilterCache';

export interface SendPhotosPageOptions {
  ctx: MyContext;
  filter: FilterDto;
  page: number;
  edit?: boolean;
  fallbackMessage: string;
  buildCallbackData: (page: number) => string;
  saveLastFilterSource?: 'search' | 'ai';
}

type PhotosByFolder = Map<string, PhotoItemDto[]>;
export type PhotosByYear = Map<number, PhotosByFolder>;

function getPhotoYear(takenDate: PhotoItemDto['takenDate']): number {
  if (takenDate instanceof Date) {
    return isValid(takenDate) ? getYear(takenDate) : 0;
  }

  if (typeof takenDate === 'string') {
    const parsed = parseISO(takenDate);
    if (isValid(parsed)) {
      return getYear(parsed);
    }
  }

  return 0;
}

export function groupPhotosByYearAndFolder(
  photos: PhotoItemDto[] | undefined,
): PhotosByYear {
  const byYear: PhotosByYear = new Map();

  if (!photos?.length) {
    return byYear;
  }

  for (const photo of photos) {
    const year = getPhotoYear(photo.takenDate);

    let yearMap = byYear.get(year);
    if (!yearMap) {
      yearMap = new Map();
      byYear.set(year, yearMap);
    }

    const folderKey = `${photo.storageName} / ${photo.relativePath}`;
    if (!yearMap.has(folderKey)) {
      yearMap.set(folderKey, []);
    }

    yearMap.get(folderKey)!.push(photo);
  }

  return byYear;
}

interface BuildSectionsResult {
  sections: string[];
  orderedPhotos: PhotoItemDto[];
}

export function buildPhotoSections(
  ctx: MyContext,
  groupedPhotos: PhotosByYear,
): BuildSectionsResult {
  const sections: string[] = [];
  const orderedPhotos: PhotoItemDto[] = [];

  const sortedByYear = [...groupedPhotos.entries()].sort(([a], [b]) => b - a);

  for (const [year, folders] of sortedByYear) {
    sections.push(`\n📅 <b>${year || ctx.t('unknown-year')}</b>`);

    for (const [folder, photos] of folders.entries()) {
      sections.push(`📁 ${folder}`);

      for (const photo of photos) {
        const title = photo.name.slice(0, 10) || ctx.t('untitled');
        const peopleCount = photo.persons?.length ?? 0;
        const isAdult = photo.isAdultContent ? '🔞' : '';
        const isRacy = photo.isRacyContent ? '⚠️' : '';

        const metaParts: string[] = [];
        if (peopleCount > 0) {
          metaParts.push(ctx.t('people-count', { count: peopleCount }));
        }
        if (isAdult) metaParts.push(isAdult);
        if (isRacy) metaParts.push(isRacy);

        const metaLine = metaParts.length ? `\n${metaParts.join(' ')}` : '';
        const captionsText = photo.captions?.join(' ') ?? '';
        const index = orderedPhotos.length + 1;

        sections.push(
          `[${index}] <b>${title}</b>\n${firstNWords(captionsText, 5)} ${metaLine}`,
        );
        captionCache.set(photo.id, captionsText);
        orderedPhotos.push(photo);
      }
    }
  }

  return { sections, orderedPhotos };
}

interface BuildKeyboardOptions {
  photos: PhotoItemDto[];
  page: number;
  totalPages: number;
  buildCallbackData: (page: number) => string;
  ctx: MyContext;
}

export function buildPhotoKeyboardAndIds({
  photos,
  page,
  totalPages,
  buildCallbackData,
  ctx,
}: BuildKeyboardOptions) {
  const keyboard = new InlineKeyboard();
  const photoIds: number[] = [];

  photos.forEach((photo, index) => {
    if (index % 5 === 0 && index !== 0) {
      keyboard.row();
    }

    keyboard.text(String(index + 1), `photo:${photo.id}`);
    photoIds.push(photo.id);
  });

  keyboard.row();

  if (page > 1) {
    keyboard.text(ctx.t('first-page'), buildCallbackData(1));
    keyboard.text(ctx.t('prev-page'), buildCallbackData(page - 1));
  }

  if (page < totalPages) {
    keyboard.text(ctx.t('next-page'), buildCallbackData(page + 1));
    keyboard.text(ctx.t('last-page'), buildCallbackData(totalPages));
  }

  return { keyboard, photoIds };
}

export async function sendPhotosPage({
  ctx,
  filter,
  page,
  edit = false,
  fallbackMessage,
  buildCallbackData,
  saveLastFilterSource,
}: SendPhotosPageOptions) {
  const chatId = ctx.chat?.id;
  if (chatId) {
    const prev = currentPagePhotos.get(chatId);
    if (!edit || (prev && prev.page !== page)) {
      await deletePhotoMessage(ctx);
    }
  }

  const pagedFilter = withPagination(filter, page);

  if (chatId && saveLastFilterSource) {
    setLastFilter(chatId, pagedFilter, saveLastFilterSource);
  }

  let queryResult;
  try {
    queryResult = await searchPhotos(ctx, pagedFilter);
  } catch (err: unknown) {
    await handleCommandError(ctx, err);
    return;
  }

  if (!queryResult.totalCount || !queryResult.items?.length) {
    if (edit) {
      await ctx.editMessageText(fallbackMessage).catch(() => ctx.reply(fallbackMessage));
    } else {
      await ctx.reply(fallbackMessage);
    }
    return;
  }
  const items = queryResult.items ?? [];
  const totalCount = queryResult.totalCount ?? 0;
  const pageSize = Math.max(pagedFilter.pageSize ?? 1, 1);
  const totalPages = Math.ceil(totalCount / pageSize);

  const groupedPhotos = groupPhotosByYearAndFolder(items);
  const { sections, orderedPhotos } = buildPhotoSections(ctx, groupedPhotos);
  const { keyboard, photoIds } = buildPhotoKeyboardAndIds({
    photos: orderedPhotos,
    page,
    totalPages,
    buildCallbackData,
    ctx,
  });

  if (chatId) {
    currentPagePhotos.set(chatId, { page, ids: photoIds });
  }

  sections.push(`\n${ctx.t('page-info', { page, total: totalPages })}`);
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
