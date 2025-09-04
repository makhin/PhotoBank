import type { Context } from 'grammy';
import type { FilterDto } from '@photobank/shared/api/photobank';

import { getPhotos } from '../api/photobank/photos/photos';
import { setRequestContext } from '../api/axios-instance';
import { handleServiceError } from '../errorHandler';

const { photosSearchPhotos, photosGetPhoto, photosUpload } = getPhotos();

export async function searchPhotos(
  ctx: Context,
  filter: FilterDto & { top?: number; skip?: number },
) {
  try {
    setRequestContext(ctx);
    const res = await photosSearchPhotos(filter as FilterDto);
    return res.data;
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getPhoto(ctx: Context, id: number) {
  try {
    setRequestContext(ctx);
    const res = await photosGetPhoto(id);
    return res.data;
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export type UploadFile = { data: BlobPart | ArrayBuffer | Uint8Array; name: string };

export async function uploadPhotos(
  ctx: Context,
  options: { files: UploadFile[]; storageId: number; path: string },
) {
  const { files, storageId, path } = options;
  const blobs = files.map(({ data, name }) => new File([data as BlobPart], name));
  try {
    setRequestContext(ctx);
    return await photosUpload({ files: blobs, storageId, path });
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
