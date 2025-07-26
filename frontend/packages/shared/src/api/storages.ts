import type { StorageDto } from '../generated';
import { StoragesService } from '../generated';

export const getAllStorages = async (): Promise<StorageDto[]> => {
  return StoragesService.getApiStorages();
};
