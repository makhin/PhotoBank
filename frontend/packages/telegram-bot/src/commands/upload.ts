import { Context } from 'grammy';
import { PhotosService } from '@photobank/shared/generated';
import {
  uploadFailedMsg,
  uploadSuccessMsg,
  uploadStorageName,
} from '@photobank/shared/constants';

import { getStorageId } from '../dictionaries';
import { getUploadFiles } from '../fileAdapter';

export async function uploadCommand(ctx: Context) {
  try {
    const uploadFiles = await getUploadFiles(ctx);

    if (!uploadFiles.length) {
      await ctx.reply(uploadFailedMsg);
      return;
    }

    const storageId = getStorageId(uploadStorageName);
    const username = ctx.from?.username ?? String(ctx.from?.id ?? '');

    await PhotosService.postApiPhotosUpload({
      files: uploadFiles,
      storageId: storageId,
      path: username,
    });

    await ctx.reply(uploadSuccessMsg);
  } catch (err) {
    await ctx.reply(uploadFailedMsg + (err instanceof Error ? `: ${err.message}` : ''));
  }
}

export const upload = uploadCommand;

