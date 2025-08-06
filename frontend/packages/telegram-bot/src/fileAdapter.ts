import axios from 'axios';
import { File } from 'fetch-blob/file.js';
import type { Context } from 'grammy';
import { BOT_TOKEN } from './config';

async function fetchFileBuffer(ctx: Context, fileId: string, fileName: string) {
  const file = await ctx.api.getFile(fileId);
  if (!file.file_path) throw new Error('file path missing');
  const url = `https://api.telegram.org/file/bot${BOT_TOKEN}/${file.file_path}`;
  const res = await axios.get<ArrayBuffer>(url, { responseType: 'arraybuffer' });
  return { buffer: Buffer.from(res.data), name: fileName };
}

/**
 * Adapter to download files from Telegram messages and convert them to File objects.
 */
export async function getUploadFiles(ctx: Context): Promise<File[]> {
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

  const buffers = await Promise.all(files);
  return buffers.map(({ buffer, name }) => new File([buffer], name));
}
