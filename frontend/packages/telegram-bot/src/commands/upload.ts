import { Context } from 'grammy';
import axios from 'axios';
import { uploadPhotosAdapter } from '@photobank/shared';
import {
  uploadFailedMsg,
  uploadSuccessMsg,
  uploadStorageName,
} from '@photobank/shared/constants';

import { BOT_TOKEN } from '../config';
import { getStorageId } from '../dictionaries';

async function fetchFileBuffer(ctx: Context, fileId: string, fileName: string) {
  console.log(`Fetching file buffer for ${fileName} (id: ${fileId})`);
  const file = await ctx.api.getFile(fileId);
  if (!file.file_path) throw new Error('file path missing');
  const url = `https://api.telegram.org/file/bot${BOT_TOKEN}/${file.file_path}`;
  console.log(`Downloading file from ${url}`);
  const res = await axios.get<ArrayBuffer>(url, { responseType: 'arraybuffer' });
  console.log(`Fetched ${res.data.byteLength} bytes for ${fileName}`);
  return { buffer: Buffer.from(res.data), name: fileName };
}

export async function uploadCommand(ctx: Context) {
  try {
    console.log('Upload command invoked by', ctx.from?.username ?? ctx.from?.id);
    const files: Array<Promise<{ buffer: Buffer; name: string }>> = [];

    if (ctx.message?.photo?.length) {
      const photo = ctx.message.photo[ctx.message.photo.length - 1];
      console.log(`Photo detected: ${photo.file_id}`);
      files.push(fetchFileBuffer(ctx, photo.file_id, `${photo.file_unique_id}.jpg`));
    }

    if (ctx.message?.document) {
      const doc = ctx.message.document;
      console.log(`Document detected: ${doc.file_id}`);
      files.push(
        fetchFileBuffer(
          ctx,
          doc.file_id,
          doc.file_name || doc.file_unique_id || 'file',
        ),
      );
    }

    if (!files.length) {
      console.warn('No files found in message');
      await ctx.reply(uploadFailedMsg);
      return;
    }

    const uploadFiles = await Promise.all(files);
    console.log('Prepared files for upload:', uploadFiles.map(f => ({ name: f.name, size: f.buffer.length })));

    const storageId = getStorageId(uploadStorageName);
    const username = ctx.from?.username ?? String(ctx.from?.id ?? '');
    console.log(`Uploading ${uploadFiles.length} files to storage ${storageId} under path ${username}`);

    await uploadPhotosAdapter({
      files: uploadFiles,
      storageId,
      path: username,
    });
    console.log('Upload API call completed successfully');

    await ctx.reply(uploadSuccessMsg);
  } catch (err) {
    console.error('Upload command failed', err);
    await ctx.reply(uploadFailedMsg + (err instanceof Error ? `: ${err.message}` : ''));
  }
}

export const upload = uploadCommand;

