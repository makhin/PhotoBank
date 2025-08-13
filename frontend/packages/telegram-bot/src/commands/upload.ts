import { Context } from 'grammy';
import axios from 'axios';
import { uploadFailedMsg, uploadSuccessMsg, uploadStorageName } from '@photobank/shared/constants';

import { uploadPhotos } from '../services/photo';
import { handleCommandError } from '../errorHandler';
import { BOT_TOKEN } from '../config';
import { getStorageId } from '../dictionaries';

async function fetchFileBuffer(ctx: Context, fileId: string, fileName: string) {
  const file = await ctx.api.getFile(fileId);
  if (!file.file_path) throw new Error('file path missing');
  const url = `https://api.telegram.org/file/bot${BOT_TOKEN}/${file.file_path}`;
  const res = await axios.get<ArrayBuffer>(url, { responseType: 'arraybuffer' });
  return { buffer: Buffer.from(res.data), name: fileName };
}

export async function uploadCommand(ctx: Context) {
  try {
    const files: Array<Promise<{ buffer: Buffer; name: string }>> = [];

    if (ctx.message?.photo?.length) {
      const photo = ctx.message.photo[ctx.message.photo.length - 1];
      files.push(fetchFileBuffer(ctx, photo.file_id, `${photo.file_unique_id}.jpg`));
    }

    if (ctx.message?.document) {
      const doc = ctx.message.document;
      files.push(
        fetchFileBuffer(
          ctx,
          doc.file_id,
          doc.file_name || doc.file_unique_id || 'file',
        ),
      );
    }

    if (!files.length) {
      await ctx.reply(uploadFailedMsg);
      return;
    }

    const uploadFiles = await Promise.all(files);

    const storageId = getStorageId(uploadStorageName);
    const username = ctx.from?.username ?? String(ctx.from?.id ?? '');

    await uploadPhotos({
      files: uploadFiles,
      storageId,
      path: username,
    });

    await ctx.reply(uploadSuccessMsg);
  } catch (err) {
    await ctx.reply(uploadFailedMsg);
    await handleCommandError(ctx, err);
  }
}

export const upload = uploadCommand;

