import Dexie, { type Table } from 'dexie';
import type { PhotoItemDto, PhotoDto } from '../types';

export interface CachedPhotoItem extends PhotoItemDto {
  added: number;
}

export interface CachedPhoto extends PhotoDto {
  added: number;
}

class PhotoCacheDb extends Dexie {
  photoItems!: Table<CachedPhotoItem, number>;
  photos!: Table<CachedPhoto, number>;

  constructor() {
    super('photobank-cache');
    this.version(1).stores({
      photoItems: 'id,added',
      photos: 'id,added',
    });
  }
}

const db = new PhotoCacheDb();

export async function cachePhotoItem(item: PhotoItemDto): Promise<void> {
  await db.photoItems.put({ ...item, added: Date.now() });
}

export async function getCachedPhotoItem(id: number): Promise<CachedPhotoItem | undefined> {
  return db.photoItems.get(id);
}

export async function cachePhoto(photo: PhotoDto): Promise<void> {
  await db.photos.put({ ...photo, added: Date.now() });
}

export async function getCachedPhoto(id: number): Promise<CachedPhoto | undefined> {
  return db.photos.get(id);
}
