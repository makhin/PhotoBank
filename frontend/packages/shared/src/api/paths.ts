import type { PathDto } from '../generated';
import { PathsService } from '../generated';

export const getAllPaths = async (): Promise<PathDto[]> => {
  return PathsService.getApiPaths();
};
