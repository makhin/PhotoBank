import type { Context } from 'grammy';
import { uploadPhotosAdapter, type UploadFile } from '@photobank/shared';
import type { FilterDto } from '@photobank/shared/api/photobank';

import {
  getPhotos,
  type PhotosGetPhotoResult,
  type PhotosSearchPhotosResult,
} from '../api/photobank/photos/photos';
import { callWithContext } from './call-with-context';

const { photosSearchPhotos, photosGetPhoto } = getPhotos();

export async function searchPhotos(ctx: Context, filter: FilterDto): Promise<PhotosSearchPhotosResult> {
  return callWithContext(ctx, photosSearchPhotos, filter);
}

export async function getPhoto(
  ctx: Context,
  id: number,
): Promise<PhotosGetPhotoResult> {
  return callWithContext(ctx, photosGetPhoto, id);
}

export async function uploadPhotos(
  ctx: Context,
  options: { files: UploadFile[]; storageId: number; path: string },
) {
  const { files, storageId, path } = options;
  return callWithContext(ctx, uploadPhotosAdapter, { files, storageId, path });
}
