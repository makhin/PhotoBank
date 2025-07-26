import type { FilterDto, PhotoDto, QueryResult } from '../generated';
import { PhotosService } from '../generated';
import { isBrowser } from '../config';
import { cachePhoto, getCachedPhoto } from '../cache/photosCache';
import { cacheFilterResult, getCachedFilterResult } from '../cache/filterResultsCache';
import { getFilterHash } from '../utils/getFilterHash';

export const searchPhotos = async (filter: FilterDto): Promise<QueryResult> => {
  const hash = await getFilterHash(filter);

  if (isBrowser()) {
    const cached = await getCachedFilterResult(hash);
    if (cached) {
      return { count: cached.count, photos: cached.photos };
    }
  }

  const result = await PhotosService.postApiPhotosSearch(filter);
  if (result.photos) {
    void cacheFilterResult(hash, {
      count: result.count ?? 0,
      photos: result.photos ?? [],
    });
  }
  return result;
};

export const getPhotoById = async (id: number): Promise<PhotoDto> => {
  if (isBrowser()) {
    const cached = await getCachedPhoto(id);
    if (cached) return cached;
  }
  const result = await PhotosService.getApiPhotos(id);
  if (isBrowser()) {
    void cachePhoto(result);
  }
  return result;
};
