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

// Send a single photo with cached file_id
export async function sendPhotoSmart(ctx: Context, p: PhotoItemDto) {
  if (!ctx.chat) {
    throw new Error('Chat not found');
  }
  const chatId = ctx.chat.id;
  const cached = getFileId(p.id);
  try {
    const message = await withTelegramRetry(() =>
      throttled(() =>
        ctx.api.sendPhoto(chatId, cached ?? (p.thumbnailUrl ?? ''), {
          caption: buildCaption(p),
        }),
      ),
    );
    const newId = message.photo?.at(-1)?.file_id || null;
    if (newId && newId !== cached) setFileId(p.id, newId);
    return message;
  } catch (e: unknown) {
    // If cached file_id expired, remove and retry with thumbnail URL
    const err = e as { error_code?: number; description?: string };
    const code = err.error_code;
    const desc = err.description ?? '';
    if (cached && (code === 400 || desc.includes('wrong file identifier'))) {
      delFileId(p.id);
      logger.warn('file_id invalidated, retry with thumbnail URL', { photoId: p.id });
      const message = (await withTelegramRetry(() =>
        throttled(() =>
          ctx.api.sendPhoto(chatId, p.thumbnailUrl ?? '', {
            caption: buildCaption(p),
          }),
        ),
      ));
      const newId = message.photo?.at(-1)?.file_id || null;
      if (newId) setFileId(p.id, newId);
      return message;
    }
    throw e;
  }
}

// Split array into chunks of N
function chunk<T>(arr: T[], size: number): T[][] {
  const out: T[][] = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

// Send album (media group) with cached file_id
export async function sendAlbumSmart(ctx: Context, photos: PhotoItemDto[]) {
  if (!ctx.chat) {
    throw new Error('Chat not found');
  }
  const chatId = ctx.chat.id;
  const groups = chunk(photos, 10); // Telegram limit
  const results: Message[] = [];

  for (const group of groups) {
    const medias: InputMediaPhoto[] = group.map((p) => {
      const cached = getFileId(p.id);
      const media = cached ?? (p.thumbnailUrl ?? '');
      return {
        type: 'photo',
        media,
        caption: buildCaption(p),
      };
    });

    try {
      // mediaGroup cannot be edited, so just send and continue
      const msgs = (await withTelegramRetry(() =>
        throttled(() => ctx.api.sendMediaGroup(chatId, medias)),
      )) as Message.PhotoMessage[];
      // Save file_id for all items where it appears
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

      // Fallback: send one by one to still deliver to user
      for (const p of group) {
        try {
          const msg = await sendPhotoSmart(ctx, p);
          results.push(msg);
        } catch (inner: unknown) {
          // if even single send fails—log and continue
          logger.error('single send failed', inner);
        }
      }
    }

    // Small pause between groups
    await new Promise((r) => setTimeout(r, 150));
  }

  return results;
}
