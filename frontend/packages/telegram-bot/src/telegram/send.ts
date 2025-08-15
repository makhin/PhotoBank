import type { Context, InputMediaPhoto } from 'grammy';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { formatDate } from '@photobank/shared/format';

import { getCachedFileId, setCachedFileId } from '../cache/fileIdCache';
import { getTagName } from '../dictionaries';

function buildCaption(photo: PhotoItemDto): string {
  const parts: string[] = [];
  parts.push(`📸 <b>${photo.name}</b>`);
  if (photo.takenDate) {
    parts.push(`📅 ${formatDate(photo.takenDate)}`);
  }
  if (photo.tags?.length) {
    const tags = photo.tags
      .map(t => getTagName(t.tagId))
      .filter(Boolean)
      .join(', ');
    if (tags) parts.push(`🏷️ ${tags}`);
  }
  return parts.join('\n');
}

export async function sendPhotoSmart(ctx: Context, photo: PhotoItemDto) {
  const caption = buildCaption(photo);
  const cached = getCachedFileId(photo.id);
  if (cached) {
    return ctx.replyWithPhoto(cached, { caption, parse_mode: 'HTML' });
  }
  const msg = await ctx.replyWithPhoto({ url: photo.previewUrl ?? photo.originalUrl ?? '' }, {
    caption,
    parse_mode: 'HTML',
  });
  const fileId = msg.photo?.[msg.photo.length - 1]?.file_id;
  if (fileId) setCachedFileId(photo.id, fileId);
  return msg;
}

export async function sendAlbumSmart(ctx: Context, photos: PhotoItemDto[]) {
  const chunks: PhotoItemDto[][] = [];
  for (let i = 0; i < photos.length; i += 10) {
    chunks.push(photos.slice(i, i + 10));
  }

  for (const chunk of chunks) {
    const media: InputMediaPhoto[] = chunk.map(photo => {
      const caption = buildCaption(photo);
      const cached = getCachedFileId(photo.id);
      if (cached) {
        return { type: 'photo', media: cached, caption, parse_mode: 'HTML' } as InputMediaPhoto;
      }
      return {
        type: 'photo',
        media: photo.previewUrl ?? photo.originalUrl ?? '',
        caption,
        parse_mode: 'HTML',
      } as InputMediaPhoto;
    });

    try {
      const res = await ctx.replyWithMediaGroup(media);
      res.forEach((msg, idx) => {
        const fileId = msg.photo?.[msg.photo.length - 1]?.file_id;
        if (fileId) setCachedFileId(chunk[idx].id, fileId);
      });
    } catch {
      for (const [idx, m] of media.entries()) {
        const msg = await ctx.replyWithPhoto(m.media, { caption: m.caption, parse_mode: 'HTML' });
        const fileId = msg.photo?.[msg.photo.length - 1]?.file_id;
        if (fileId) setCachedFileId(chunk[idx].id, fileId);
      }
    }

    await new Promise(r => setTimeout(r, 100));
  }
}
