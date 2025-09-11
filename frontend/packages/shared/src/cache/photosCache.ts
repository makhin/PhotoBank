import Dexie, { type Table } from 'dexie';
import { LRUCache } from 'lru-cache';

import { isBrowser } from '../utils/isBrowser.js';
import type { PhotoDto } from '../api/photobank';

export interface CachedPhoto {
  id: number;
  previewUrl?: string | null;
  added: number;
}

class PhotoCacheDb extends Dexie {
  photos!: Table<CachedPhoto, number>;

  constructor() {
    super('photobank-cache');
    this.version(1).stores({
      photos: 'id,added',
    });
  }
}

let db: PhotoCacheDb | undefined;
let photoCache: LRUCache<number, CachedPhoto> | undefined;

if (isBrowser()) {
  db = new PhotoCacheDb();
} else {
  photoCache = new LRUCache({ max: 100 });
}

export async function cachePhoto(photo: PhotoDto): Promise<void> {
  const { id, previewUrl } = photo;
  const cached: CachedPhoto = { id, previewUrl, added: Date.now() };
  if (isBrowser()) {
    await db?.photos.put(cached);
  } else {
    photoCache?.set(id, cached);
  }
}

export async function getCachedPhoto(id: number): Promise<CachedPhoto | undefined> {
  if (isBrowser()) {
    if (!db) return Promise.resolve(undefined);
    return db.photos.get(id);
  }
  return Promise.resolve(photoCache?.get(id));
}
