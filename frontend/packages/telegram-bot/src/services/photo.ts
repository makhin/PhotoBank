import { postApiPhotosSearch, getApiPhotos } from '@photobank/shared/api/photobank';
import { uploadPhotosAdapter } from '@photobank/shared';
import type { FilterDto } from '@photobank/shared/api/photobank';
import { handleServiceError } from '../errorHandler';

export async function searchPhotos(filter: FilterDto & { top?: number; skip?: number }) {
  try {
    return await postApiPhotosSearch(filter);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getPhoto(id: number) {
  try {
    return await getApiPhotos(id);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function uploadPhotos(options: Parameters<typeof uploadPhotosAdapter>[0]) {
  try {
    return await uploadPhotosAdapter(options);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
