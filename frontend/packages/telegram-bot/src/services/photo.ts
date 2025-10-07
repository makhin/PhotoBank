import type { Context } from 'grammy';
import { type UploadFile } from '@photobank/shared';
import type { FilterDto, PhotoDto, PhotoItemDtoPageResponse } from '@photobank/shared/api/photobank';

import { photosGetPhoto, photosSearchPhotos, photosUpload } from '../api/photobank/photos/photos';
import { callWithContext } from './call-with-context';

function normalizeToBlobPart(data: UploadFile['data']): BlobPart {
  if (ArrayBuffer.isView(data)) {
    const { buffer, byteOffset, byteLength } = data;
    if (buffer instanceof ArrayBuffer) {
      return buffer.slice(byteOffset, byteOffset + byteLength);
    }

    const copy = new Uint8Array(byteLength);
    copy.set(new Uint8Array(buffer, byteOffset, byteLength));
    return copy.buffer;
  }

  if (data instanceof ArrayBuffer) {
    return data;
  }

  return data;
}

export async function searchPhotos(
  ctx: Context,
  filter: FilterDto,
): Promise<PhotoItemDtoPageResponse> {
  const response = await callWithContext(ctx, () => photosSearchPhotos(filter));
  return response.data!;
}

export async function getPhoto(
  ctx: Context,
  id: number,
): Promise<PhotoDto> {
  const response = await callWithContext(ctx, () => photosGetPhoto(id));
  return response.data!;
}

export async function uploadPhotos(
  ctx: Context,
  options: { files: UploadFile[]; storageId: number; path: string },
) {
  const { files, storageId, path } = options;
  const normalizedFiles = files.map(({ data, name }) => {
    const blob = normalizeToBlobPart(data);
    return new File([blob], name);
  });

  await callWithContext(ctx, () => photosUpload({ files: normalizedFiles, storageId, path }));
}
