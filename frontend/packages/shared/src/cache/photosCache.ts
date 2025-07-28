import Dexie, { type Table } from 'dexie';
import { LRUCache } from 'lru-cache';
import { isBrowser } from '../utils/isBrowser';
import type { PhotoDto } from '../generated';

export interface CachedPhoto extends PhotoDto {
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
