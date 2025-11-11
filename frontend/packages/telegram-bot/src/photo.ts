import { Buffer } from 'node:buffer';
import { InlineKeyboard, InputFile } from 'grammy';
import type { PhotoDto } from '@photobank/shared/api/photobank';

import { formatPhotoMessage } from './formatPhotoMessage';
import type { MyContext } from './i18n';
import { getPhoto } from './services/photo';
import { IMAGE_BASE_URL, API_BASE_URL } from './config';

export const photoMessages = new Map<number, number>();
export const currentPagePhotos = new Map<
  number,
  { page: number; ids: number[] }
>();
export const captionCache = new Map<number, string>();

function extractFilenameFromUrl(url: string): string | undefined {
  try {
    const parsed = new URL(url);
    const segments = parsed.pathname.split('/').filter(Boolean);
    if (!segments.length) return undefined;
    return decodeURIComponent(segments[segments.length - 1]!);
  } catch {
    const path = url.split('?')[0] ?? '';
    const segment = path.split('/').filter(Boolean).pop();
    return segment ? decodeURIComponent(segment) : undefined;
  }
}

function deriveFilename(photo: PhotoDto, imageUrl: string): string | undefined {
  const name = photo.name?.trim();
  if (name) {
    if (name.includes('.')) return name;
    const extCandidate = extractFilenameFromUrl(imageUrl);
    if (extCandidate) {
      const dotIndex = extCandidate.lastIndexOf('.');
      if (dotIndex !== -1) {
        return `${name}${extCandidate.slice(dotIndex)}`;
      }
    }
    return name;
  }
  return extractFilenameFromUrl(imageUrl);
}

function resolveImageUrl(imageUrl: string): string {
  // Если URL относительный (начинается с /), преобразуем его в абсолютный
  if (imageUrl.startsWith('/')) {
    // Используем IMAGE_BASE_URL, если задан, иначе fallback на API_BASE_URL для обратной совместимости
    const baseUrl = IMAGE_BASE_URL || API_BASE_URL || '';
    // Убираем trailing slash из baseUrl, если есть
    const normalizedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    return `${normalizedBase}${imageUrl}`;
  }
  // Если URL уже абсолютный, возвращаем как есть
  return imageUrl;
}

export async function loadPhotoFile(photo: PhotoDto): Promise<{
  caption: string;
  hasSpoiler: boolean;
  photoFile?: InputFile;
}> {
  const { caption, hasSpoiler, imageUrl } = await formatPhotoMessage(photo);
  if (!imageUrl) {
    return { caption, hasSpoiler };
  }

  try {
    // Преобразуем относительный URL в абсолютный для серверного fetch
    const absoluteUrl = resolveImageUrl(imageUrl);
    const response = await fetch(absoluteUrl);
    if (!response.ok) {
      throw new Error(`Failed to load preview: ${response.status}`);
    }
    const arrayBuffer = await response.arrayBuffer();
    const filename = deriveFilename(photo, imageUrl);
    const photoFile = new InputFile(Buffer.from(arrayBuffer), filename);
    return { caption, hasSpoiler, photoFile };
  } catch {
    return { caption, hasSpoiler };
  }
}

export async function deletePhotoMessage(ctx: MyContext) {
  const chatId = ctx.chat?.id;
  const messageId = chatId ? photoMessages.get(chatId) : undefined;
  if (chatId && messageId) {
    try {
      await ctx.api.deleteMessage(chatId, messageId);
    } catch {
      // ignore
    }
    photoMessages.delete(chatId);
  }
}

async function fetchPhoto(
  ctx: MyContext,
  id: number
): Promise<PhotoDto | null> {
  try {
    return await getPhoto(ctx, id);
  } catch {
    return null;
  }
}

export async function sendPhotoById(ctx: MyContext, id: number) {
  let photo: PhotoDto;

  try {
    photo = await getPhoto(ctx, id);
  } catch {
    await ctx.reply(ctx.t('photo-not-found'));
    return;
  }

  const { caption, hasSpoiler, photoFile } = await loadPhotoFile(photo);

  if (photoFile) {
    await ctx.replyWithPhoto(photoFile, {
      caption,
      parse_mode: 'HTML',
      has_spoiler: hasSpoiler,
    });
    return;
  }

  await ctx.reply(caption, { parse_mode: 'HTML' });
}

export async function openPhotoInline(ctx: MyContext, id: number) {
  const chatId = ctx.chat?.id;
  const photo = await fetchPhoto(ctx, id);
  if (!photo) {
    await ctx.reply(ctx.t('photo-not-found'));
    return;
  }

  const { caption, hasSpoiler, photoFile } = await loadPhotoFile(photo);

  let keyboard: InlineKeyboard | undefined;
  if (chatId) {
    const list = currentPagePhotos.get(chatId);
    if (list) {
      const index = list.ids.indexOf(id);
      keyboard = new InlineKeyboard();
      if (index > 0)
        keyboard.text(ctx.t('prev-page'), `photo_nav:${list.ids[index - 1]}`);
      if (index < list.ids.length - 1)
        keyboard.text(ctx.t('next-page'), `photo_nav:${list.ids[index + 1]}`);
      if (!keyboard.inline_keyboard.length) keyboard = undefined;
    }
  }

  if (!chatId) {
    if (photoFile) {
      const options: Parameters<typeof ctx.replyWithPhoto>[1] = {
        caption,
        parse_mode: 'HTML',
        has_spoiler: hasSpoiler,
      };
      if (keyboard) options.reply_markup = keyboard;
      await ctx.replyWithPhoto(photoFile, options);
    } else {
      const options: Parameters<typeof ctx.reply>[1] = { parse_mode: 'HTML' };
      if (keyboard) options.reply_markup = keyboard;
      await ctx.reply(caption, options);
    }
    return;
  }

  const existing = photoMessages.get(chatId);
  if (existing) {
    try {
      if (photoFile) {
        const options: Parameters<typeof ctx.api.editMessageMedia>[3] = {};
        if (keyboard) options.reply_markup = keyboard;
        await ctx.api.editMessageMedia(
          chatId,
          existing,
          {
            type: 'photo',
            media: photoFile,
            caption,
            parse_mode: 'HTML',
            has_spoiler: hasSpoiler,
          },
          options
        );
      } else {
        await ctx.api.editMessageCaption(chatId, existing, {
          caption,
          parse_mode: 'HTML',
        });
      }
      return;
    } catch {
      // Если редактирование не удалось, удаляем старое сообщение и создадим новое
      photoMessages.delete(chatId);
      try {
        await ctx.api.deleteMessage(chatId, existing);
      } catch {
        // Игнорируем ошибку удаления (сообщение может быть уже удалено)
      }
    }
  }

  if (photoFile) {
    const options: Parameters<typeof ctx.replyWithPhoto>[1] = {
      caption,
      parse_mode: 'HTML',
      has_spoiler: hasSpoiler,
    };
    if (keyboard) options.reply_markup = keyboard;
    const msg = await ctx.replyWithPhoto(photoFile, options);
    photoMessages.set(chatId, msg.message_id);
  } else {
    const options: Parameters<typeof ctx.reply>[1] = { parse_mode: 'HTML' };
    if (keyboard) options.reply_markup = keyboard;
    const msg = await ctx.reply(caption, options);
    photoMessages.set(chatId, msg.message_id);
  }
}
