import { InlineKeyboard } from 'grammy';
import type { PhotoDto } from '@photobank/shared/api/photobank';

import { formatPhotoMessage } from './formatPhotoMessage';
import type { MyContext } from './i18n';
import { getPhoto } from './services/photo';

export const photoMessages = new Map<number, number>();
export const currentPagePhotos = new Map<
  number,
  { page: number; ids: number[] }
>();
export const captionCache = new Map<number, string>();

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
    const photo = await getPhoto(ctx, id);
    return photo;
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

  const { caption, hasSpoiler, imageUrl } = formatPhotoMessage(photo);

  if (imageUrl) {
    await ctx.replyWithPhoto(imageUrl, {
      caption,
      parse_mode: 'HTML',
      has_spoiler: hasSpoiler,
    });
  } else {
    await ctx.reply(caption, { parse_mode: 'HTML' });
  }
}

export async function openPhotoInline(ctx: MyContext, id: number) {
  const chatId = ctx.chat?.id;
  const photo = await fetchPhoto(ctx, id);
  if (!photo) {
    await ctx.reply(ctx.t('photo-not-found'));
    return;
  }

  const { caption, hasSpoiler, imageUrl } = formatPhotoMessage(photo);

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
    if (imageUrl) {
      const options: Parameters<typeof ctx.replyWithPhoto>[1] = {
        caption,
        parse_mode: 'HTML',
        has_spoiler: hasSpoiler,
      };
      if (keyboard) options.reply_markup = keyboard;
      await ctx.replyWithPhoto(imageUrl, options);
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
      if (imageUrl) {
        const options: Parameters<typeof ctx.api.editMessageMedia>[3] = {};
        if (keyboard) options.reply_markup = keyboard;
        await ctx.api.editMessageMedia(
          chatId,
          existing,
          {
            type: 'photo',
            media: imageUrl,
            caption,
            parse_mode: 'HTML',
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
      photoMessages.delete(chatId);
    }
  }

  if (imageUrl) {
    const options: Parameters<typeof ctx.replyWithPhoto>[1] = {
      caption,
      parse_mode: 'HTML',
      has_spoiler: hasSpoiler,
    };
    if (keyboard) options.reply_markup = keyboard;
    const msg = await ctx.replyWithPhoto(imageUrl, options);
    photoMessages.set(chatId, msg.message_id);
  } else {
    const options: Parameters<typeof ctx.reply>[1] = { parse_mode: 'HTML' };
    if (keyboard) options.reply_markup = keyboard;
    const msg = await ctx.reply(caption, options);
    photoMessages.set(chatId, msg.message_id);
  }
}
