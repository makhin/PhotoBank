import type { TagDto } from '../generated';
import { TagsService } from '../generated';

export const getAllTags = async (): Promise<TagDto[]> => {
  return TagsService.getApiTags();
};
