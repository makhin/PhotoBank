// packages/shared/src/index.ts

export const getGenderText = (gender?: boolean | null) => {
  if (gender === undefined || gender === null) return 'не указан пол';
  return gender ? 'Муж' : 'Жен';
};

export { getFilterHash } from './utils/getFilterHash';
export { getOrientation } from './utils/getOrientation';
export {
  cachePhoto,
  getCachedPhoto,
} from './cache/photosCache';
export {
  cacheFilterResult,
  getCachedFilterResult,
} from './cache/filterResultsCache';

export const firstNWords = (sentence: string, count: number): string => {
  const trimmed = sentence.trim();
  if (trimmed === '') return '';

  const words = trimmed.split(/\s+/);
  if (words.length <= count) {
    return trimmed + ' ';
  }

  return words.slice(0, count).join(' ') + '... ';
};

export { useIsAdmin } from './hooks/useIsAdmin';
export { useCanSeeNsfw } from './hooks/useCanSeeNsfw';
export { getPlaceByGeoPoint } from './utils/geocode';
export { uploadPhotosAdapter, type UploadFile, type UploadFileData } from './adapters/photos-upload.adapter';

export * from './format';
export * from './auth';
export * from './safeStorage';
export * from './constants';
export * as logger from './utils/logger';

// важно: пробрасываем наружу весь автосгенерённый API под @photobank/shared/api/photobank
export * as api from './api/photobank';
export * from './api/photobank';

export { configureApi, configureApiAuth, setImpersonateUser } from './api/photobank/fetcher';
export { configureApi as setBaseUrl } from './api/photobank/fetcher';