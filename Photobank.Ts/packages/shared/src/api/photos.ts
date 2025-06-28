import { FilterDto, PhotoDto, QueryResult } from '../types';
import { apiClient } from './client';

export const searchPhotos = async (filter: FilterDto): Promise<QueryResult> => {
  const response = await apiClient.post('/photos/search', filter);
  return response.data;
};

export const getPhotoById = async (id: number): Promise<PhotoDto> => {
  const response = await apiClient.get(`/photos/${id}`);
  return response.data;
};
