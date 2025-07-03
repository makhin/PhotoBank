import type { FilterDto, PhotoDto, QueryResult } from '../types';
import { apiClient } from './client';
import { isBrowser } from '../config';
import { cachePhotoItem, cachePhoto, getCachedPhoto } from '../cache/photosCache';

export const searchPhotos = async (filter: FilterDto): Promise<QueryResult> => {
  const response = await apiClient.post<QueryResult>('/photos/search', filter);
  if (isBrowser && response.data.photos) {
    for (const item of response.data.photos) {
      void cachePhotoItem(item);
    }
  }
  return response.data;
};

export const getPhotoById = async (id: number): Promise<PhotoDto> => {
  if (isBrowser) {
    const cached = await getCachedPhoto(id);
    if (cached) return cached;
  }
  const response = await apiClient.get<PhotoDto>(`/photos/${id}`);
  if (isBrowser) {
    void cachePhoto(response.data);
  }
  return response.data;
};
