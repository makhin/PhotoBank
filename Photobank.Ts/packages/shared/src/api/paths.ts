import { PathDto } from '../types';
import { apiClient } from './client';

export const getAllPaths = async (): Promise<PathDto[]> => {
  const response = await apiClient.get('/paths');
  return response.data;
};
