import { PhotoItemDto } from './PhotoItemDto';

export interface QueryResult {
  count: number;
  photos?: PhotoItemDto[];
}