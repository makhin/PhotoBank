import type { PhotoItemDto } from './PhotoItemDto';

export interface QueryResult {
  count: number;
  photos?: PhotoItemDto[];
}