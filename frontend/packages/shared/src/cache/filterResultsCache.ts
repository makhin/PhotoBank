import Dexie, { type Table } from 'dexie';
import { LRUCache } from 'lru-cache';
import { isBrowser } from '../config';
import type { PhotoItemDto } from '../types';

export interface CachedFilterResult {
  hash: string;
  count: number;
  photos: PhotoItemDto[];
  added: number;
}

class FilterResultsDb extends Dexie {
  filterResults!: Table<CachedFilterResult, string>;

  constructor() {
    super('photobank-filter-cache');
    this.version(1).stores({
      filterResults: 'hash,added',
    });
  }
}

let db: FilterResultsDb | undefined;
let memoryCache: LRUCache<string, CachedFilterResult> | undefined;

if (isBrowser()) {
  db = new FilterResultsDb();
} else {
  memoryCache = new LRUCache({ max: 100 });
}

export async function cacheFilterResult(
  hash: string,
  result: { count: number; photos: PhotoItemDto[] }
): Promise<void> {
  const cached: CachedFilterResult = { hash, count: result.count, photos: result.photos, added: Date.now() };
  if (isBrowser()) {
    await db!.filterResults.put(cached);
  } else {
    memoryCache!.set(hash, cached);
  }
}

export async function getCachedFilterResult(
  hash: string
): Promise<CachedFilterResult | undefined> {
  if (isBrowser()) {
    return db!.filterResults.get(hash);
  }
  return Promise.resolve(memoryCache!.get(hash));
}
