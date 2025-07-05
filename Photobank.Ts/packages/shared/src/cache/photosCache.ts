import Dexie, { type Table } from 'dexie';
import { LRUCache } from 'lru-cache';
import { isBrowser } from '../config';
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

let db: PhotoCacheDb | undefined;
let photoItemCache: LRUCache<number, CachedPhotoItem> | undefined;
let photoCache: LRUCache<number, CachedPhoto> | undefined;

if (isBrowser()) {
  db = new PhotoCacheDb();
} else {
  photoItemCache = new LRUCache({ max: 1000 });
  photoCache = new LRUCache({ max: 100 });
}

export async function cachePhotoItem(item: PhotoItemDto): Promise<void> {
  const cached = { ...item, added: Date.now() };
  if (isBrowser()) {
    await db!.photoItems.put(cached);
  } else {
    photoItemCache!.set(item.id, cached);
  }
}

export async function getCachedPhotoItem(id: number): Promise<CachedPhotoItem | undefined> {
  if (isBrowser()) {
    return db!.photoItems.get(id);
  }
  return Promise.resolve(photoItemCache!.get(id));
}

export async function cachePhoto(photo: PhotoDto): Promise<void> {
  const cached = { ...photo, added: Date.now() };
  if (isBrowser()) {
    await db!.photos.put(cached);
  } else {
    photoCache!.set(photo.id, cached);
  }
}

export async function getCachedPhoto(id: number): Promise<CachedPhoto | undefined> {
  if (isBrowser()) {
    return db!.photos.get(id);
  }
  return Promise.resolve(photoCache!.get(id));
}
