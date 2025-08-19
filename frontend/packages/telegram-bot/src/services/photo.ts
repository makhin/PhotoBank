import type { Context } from 'grammy';
import type { FilterDto } from '@photobank/shared/api/photobank';

import { getPhotos } from '../api/photobank/photos/photos';
import { handleServiceError } from '../errorHandler';

const { photosSearchPhotos, photosGetPhoto, photosUpload } = getPhotos();

export async function searchPhotos(
  ctx: Context,
  filter: FilterDto & { top?: number; skip?: number },
) {
  try {
    return await photosSearchPhotos(filter as FilterDto, ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getPhoto(ctx: Context, id: number) {
  try {
    return await photosGetPhoto(id, ctx);
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
    return await photosUpload({ files: blobs, storageId, path }, ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
