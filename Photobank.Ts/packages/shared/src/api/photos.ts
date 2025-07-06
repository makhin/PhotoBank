import type { FilterDto, PhotoDto, QueryResult } from '../types';
import { apiClient } from './client';
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

  const response = await apiClient.post<QueryResult>('/photos/search', filter);
  if (response.data.photos) {
    void cacheFilterResult(hash, { count: response.data.count, photos: response.data.photos });
  }
  return response.data;
};

export const getPhotoById = async (id: number): Promise<PhotoDto> => {
  if (isBrowser()) {
    const cached = await getCachedPhoto(id);
    if (cached) return cached;
  }
  const response = await apiClient.get<PhotoDto>(`/photos/${id}`);
  if (isBrowser()) {
    void cachePhoto(response.data);
  }
  return response.data;
};
