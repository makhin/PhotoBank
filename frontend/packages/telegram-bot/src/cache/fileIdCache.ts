import { LRUCache } from 'lru-cache';

type Key = number; // PhotoId
type Val = string; // file_id

const cache = new LRUCache<Key, Val>({
  max: 20_000,
  ttl: 1000 * 60 * 60 * 24 * 14, // 14 дней
});

export function getFileId(photoId: number): string | undefined {
  return cache.get(photoId);
}
export function setFileId(photoId: number, fileId: string) {
  cache.set(photoId, fileId);
}
export function delFileId(photoId: number) {
  cache.delete(photoId);
}
