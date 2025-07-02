import { StorageDto } from '../types';
import { apiClient } from './client';

export const getAllStorages = async (): Promise<StorageDto[]> => {
  const response = await apiClient.get<StorageDto[]>('/storages');
  return response.data;
};
