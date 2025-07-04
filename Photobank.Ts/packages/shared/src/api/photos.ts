import type { FilterDto, PhotoDto, QueryResult } from '../types';
import { apiClient } from './client';
import { isBrowser } from '../config';
import { cachePhotoItem, cachePhoto, getCachedPhoto } from '../cache/photosCache';
import { cacheFilterResult } from '../cache/filterResultsCache';
import { getFilterHash } from '../utils/getFilterHash';

export const searchPhotos = async (filter: FilterDto): Promise<QueryResult> => {
  const response = await apiClient.post<QueryResult>('/photos/search', filter);
  if (response.data.photos) {
    const ids = response.data.photos.map((p) => p.id);
    const hash = await getFilterHash(filter);
    void cacheFilterResult(hash, ids);
    if (isBrowser()) {
      for (const item of response.data.photos) {
        void cachePhotoItem(item);
      }
    }
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
