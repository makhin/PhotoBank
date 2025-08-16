import type { Context } from 'grammy';
import type { InputMediaPhoto, Message } from 'grammy/types';
import { formatDate } from '@photobank/shared/format';

import { throttled } from '../utils/limiter';
import { getFileId, setFileId, delFileId } from '../cache/fileIdCache';
import { logger } from '../utils/logger';
import type { PhotoItemDto } from '../types';
import { withTelegramRetry } from '../utils/retry';

function buildCaption(p: PhotoItemDto): string {
  const parts = [p.name, p.takenDate ? formatDate(p.takenDate) : null];
  return parts.filter(Boolean).join('\n');
}

// Отправка одного фото с кэшем file_id
export async function sendPhotoSmart(ctx: Context, p: PhotoItemDto) {
  const cached = getFileId(p.id);
  try {
    const message = (await withTelegramRetry(() =>
      throttled(() =>
        ctx.api.sendPhoto(
          ctx.chat.id,
          cached ?? (p.previewUrl ?? p.originalUrl ?? ''),
          { caption: buildCaption(p) },
        ),
      ),
    ));
    const newId = message.photo?.at(-1)?.file_id || null;
    if (newId && newId !== cached) setFileId(p.id, newId);
    return message;
    } catch (e: unknown) {
      // Если кэшированный file_id внезапно протух — удаляем и пробуем разок URL
      const err = e as { error_code?: number; description?: string };
      const code = err.error_code;
      const desc = err.description ?? '';
    if (cached && (code === 400 || desc.includes('wrong file identifier'))) {
      delFileId(p.id);
      logger.warn('file_id invalidated, retry with URL', { photoId: p.id });
        const message = (await withTelegramRetry(() =>
          throttled(() =>
            ctx.api.sendPhoto(
              ctx.chat.id,
              p.previewUrl ?? p.originalUrl ?? '',
              { caption: buildCaption(p) },
            ),
          ),
        ));
      const newId = message.photo?.at(-1)?.file_id || null;
      if (newId) setFileId(p.id, newId);
      return message;
    }
    throw e;
  }
}

// Разбить массив на чанки по N
function chunk<T>(arr: T[], size: number): T[][] {
  const out: T[][] = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

// Отправка альбомом (media group) с кэшем file_id
export async function sendAlbumSmart(ctx: Context, photos: PhotoItemDto[]) {
  const groups = chunk(photos, 10); // Telegram лимит
    const results: Message[] = [];

  for (const group of groups) {
    const medias: InputMediaPhoto[] = group.map((p) => {
      const cached = getFileId(p.id);
      const media = cached ?? (p.previewUrl ?? p.originalUrl ?? '');
      return {
        type: 'photo',
        media,
        caption: buildCaption(p),
      } as InputMediaPhoto;
    });

    try {
      // mediaGroup нельзя редактировать, поэтому просто шлём и идём дальше
      const msgs = (await withTelegramRetry(() =>
        throttled(() => ctx.api.sendMediaGroup(ctx.chat.id, medias)),
      )) as Message.PhotoMessage[];
      // Сохранить file_id по всем элементам, где он появился
      msgs.forEach((m, i) => {
        const p = group[i];
        const id = m.photo?.at(-1)?.file_id;
        if (id) setFileId(p.id, id);
      });
      results.push(...msgs);
      } catch (e: unknown) {
        const err = e as { error_code?: number; description?: string };
        const code = err.error_code;
        const desc = err.description ?? '';
        logger.warn('sendMediaGroup failed, fallback to singles', { code, desc, count: group.length });

      // Fallback: отправляем по одному, чтобы всё равно доставить пользователю
      for (const p of group) {
        try {
          const msg = await sendPhotoSmart(ctx, p);
          results.push(msg);
        } catch (inner) {
          // если даже одиночная упала — лог и продолжаем
          logger.error('single send failed', inner);
        }
      }
    }

    // Между группами небольшая пауза
    await new Promise((r) => setTimeout(r, 150));
  }

  return results;
}
