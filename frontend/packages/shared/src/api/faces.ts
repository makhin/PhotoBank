import { apiClient } from './client';
import type { UpdateFaceDto } from '../types';

export const updateFace = async (dto: UpdateFaceDto): Promise<void> => {
  await apiClient.put('/faces', dto);
};
