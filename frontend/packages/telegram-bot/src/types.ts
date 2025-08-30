import type { PhotoItemDto as SharedPhotoItemDto } from '@photobank/shared/api/photobank';

export interface PhotoItemDto extends SharedPhotoItemDto {
  previewImage?: string;
}
