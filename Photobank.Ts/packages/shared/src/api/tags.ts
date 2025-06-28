import { TagDto } from '../types';
import { apiClient } from './client';

export const getAllTags = async (): Promise<TagDto[]> => {
  const response = await apiClient.get('/tags');
  return response.data;
};
