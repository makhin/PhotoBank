import axios from 'axios';
import { uploadStorageName } from '@photobank/shared/constants';

import { BOT_TOKEN } from '../config';
import { getStorageId } from '../dictionaries';
import { handleCommandError } from '../errorHandler';
import type { MyContext } from '../i18n';
import { uploadPhotos } from '../services/photo';

async function fetchFile(ctx: MyContext, fileId: string, fileName: string) {
  const file = await ctx.api.getFile(fileId);
  if (!file.file_path) throw new Error('file path missing');
  const url = `https://api.telegram.org/file/bot${BOT_TOKEN}/${file.file_path}`;
  const res = await axios.get<ArrayBuffer>(url, { responseType: 'arraybuffer' });
  return { data: res.data, name: fileName };
}

export async function uploadCommand(ctx: MyContext) {
  try {
    const files: Array<Promise<{ data: ArrayBuffer; name: string }>> = [];

    const photos = ctx.message?.photo;
    if (photos?.length) {
      const last = photos.at(-1)!;
      files.push(fetchFile(ctx, last.file_id, `${last.file_unique_id}.jpg`));
    }

    if (ctx.message?.document) {
      const doc = ctx.message.document;
      files.push(
        fetchFile(
          ctx,
          doc.file_id,
          doc.file_name || doc.file_unique_id || 'file',
        ),
      );
    }

    if (!files.length) {
      await ctx.reply(ctx.t('upload-failed'));
      return;
    }

    const uploadFiles = await Promise.all(files);

    const storageId = getStorageId(uploadStorageName);
    const username = ctx.from?.username ?? String(ctx.from?.id ?? '');

    await uploadPhotos(ctx, {
      files: uploadFiles,
      storageId,
      path: username,
    });

    await ctx.reply(ctx.t('upload-success'));
  } catch (err: unknown) {
    await ctx.reply(ctx.t('upload-failed'));
    await handleCommandError(ctx, err);
  }
}

export const upload = uploadCommand;

