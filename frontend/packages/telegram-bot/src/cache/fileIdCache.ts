import LRUCache from 'lru-cache';

/**
 * Simple LRU cache for mapping Photo IDs from the Photobank API to Telegram's
 * `file_id` so that we avoid uploading the same binary multiple times.  This
 * drastically reduces both latency and traffic costs.
 */
export const fileIdCache = new LRUCache<number, string>({
  // A few thousands of entries is enough for typical usage while keeping the
  // memory footprint low.  The limit can be tuned later via configuration if
  // required.
  max: 20_000,
});

export function getCachedFileId(photoId: number): string | undefined {
  return fileIdCache.get(photoId);
}

export function setCachedFileId(photoId: number, fileId: string): void {
  fileIdCache.set(photoId, fileId);
}
