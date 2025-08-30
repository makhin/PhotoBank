import { InlineKeyboard } from 'grammy';
import type { FilterDto } from '@photobank/shared/api/photobank';
import { firstNWords } from '@photobank/shared/index';

import type { MyContext } from '../i18n';
import { searchPhotos } from '../services/photo';
import { handleCommandError } from '../errorHandler';
import { captionCache, currentPagePhotos, deletePhotoMessage } from '../photo';

export const PHOTOS_PAGE_SIZE = 10;

export interface SendPhotosPageOptions {
  ctx: MyContext;
  filter: FilterDto;
  page: number;
  edit?: boolean;
  fallbackMessage: string;
  buildCallbackData: (page: number) => string;
}

export async function sendPhotosPage({
  ctx,
  filter,
  page,
  edit = false,
  fallbackMessage,
  buildCallbackData,
}: SendPhotosPageOptions) {
  const chatId = ctx.chat?.id;
  if (chatId) {
    const prev = currentPagePhotos.get(chatId);
    if (prev && prev.page !== page) {
      await deletePhotoMessage(ctx);
    }
  }

  let queryResult;
  try {
    const res = await searchPhotos(ctx, {
      ...filter,
      page,
      pageSize: PHOTOS_PAGE_SIZE,
    });
    queryResult = res.data;
  } catch (err) {
    await handleCommandError(ctx, err);
    return;
  }

  if (!queryResult.count || !queryResult.photos?.length) {
    if (edit) {
      await ctx.editMessageText(fallbackMessage).catch(() => ctx.reply(fallbackMessage));
    } else {
      await ctx.reply(fallbackMessage);
    }
    return;
  }

  const totalPages = Math.ceil(queryResult.count / PHOTOS_PAGE_SIZE);
  const byYear = new Map<number, Map<string, typeof queryResult.photos>>();

  for (const photo of queryResult.photos) {
    const year = photo.takenDate ? new Date(photo.takenDate).getFullYear() : 0;
    if (!byYear.has(year)) byYear.set(year, new Map());
    const yearMap = byYear.get(year);
    const key = `${photo.storageName} / ${photo.relativePath}`;
    if (!yearMap.has(key)) yearMap.set(key, []);
    yearMap.get(key)!.push(photo);
  }

  const sections: string[] = [];
  const keyboard = new InlineKeyboard();
  const photoIds: number[] = [];

  [...byYear.entries()]
    .sort(([a], [b]) => b - a)
    .forEach(([year, folders]) => {
      sections.push(`\n📅 <b>${year || ctx.t('unknown-year')}</b>`);
      [...folders.entries()].forEach(([folder, photos]) => {
        sections.push(`📁 ${folder}`);
        photos.forEach((photo) => {
          const title = photo.name.slice(0, 10) || ctx.t('untitled');
          const peopleCount = photo.persons?.length ?? 0;
          const isAdult = photo.isAdultContent ? '🔞' : '';
          const isRacy = photo.isRacyContent ? '⚠️' : '';

          const metaParts: string[] = [];
          if (peopleCount > 0) metaParts.push(ctx.t('people-count', { count: peopleCount }));
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
    if (index % 5 === 0 && index !== 0) keyboard.row();
    keyboard.text(String(index + 1), `photo:${id}`);
  });

  if (chatId) {
    currentPagePhotos.set(chatId, { page, ids: photoIds });
  }

  keyboard.row();

  sections.push(`\n${ctx.t('page-info', { page, total: totalPages })}`);

  if (page > 1) {
    keyboard.text(ctx.t('first-page'), buildCallbackData(1));
    keyboard.text(ctx.t('prev-page'), buildCallbackData(page - 1));
  }
  if (page < totalPages) {
    keyboard.text(ctx.t('next-page'), buildCallbackData(page + 1));
    keyboard.text(ctx.t('last-page'), buildCallbackData(totalPages));
  }

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
