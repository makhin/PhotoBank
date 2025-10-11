// packages/shared/src/index.ts

export type GenderCategory = 'male' | 'female' | 'unknown';

export const resolveGender = (gender?: boolean | null): GenderCategory => {
  if (gender === undefined || gender === null) return 'unknown';
  return gender ? 'male' : 'female';
};

export interface GenderLabels {
  male: string;
  female: string;
  unknown: string;
}

export const formatGender = (
  gender: boolean | null | undefined,
  labels: GenderLabels,
): string => {
  const key = resolveGender(gender);
  return labels[key];
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
export {
  formatGeoLink,
  isValidGeoPoint,
  type GeoPointLike,
} from './utils/geolocation';
export { uploadPhotosAdapter, type UploadFile, type UploadFileData } from './adapters/photos-upload.adapter';

export * from './format';
export * from './auth';
export * from './safeStorage';
export * from './filter/defaults';
export * from './constants';
export * from './metadata';
export * as logger from './utils/logger';

// важно: пробрасываем наружу весь автосгенерённый API под @photobank/shared/api/photobank
export * as api from './api/photobank';
export * from './api/photobank';

export {
  configureApi,
  configureApiAuth,
  setImpersonateUser,
  runWithRequestContext,
  getRequestContext,
} from './api/photobank/fetcher';
export { configureApi as setBaseUrl } from './api/photobank/fetcher';
export {
  applyHttpContext,
  getDefaultRetryPolicy,
  getRetryPolicy,
  resetRetryPolicy,
  setRetryPolicy,
  type HttpContextConfig,
  type HttpRetryPolicy,
  type MaybeTokenManager,
  type RetryAttemptContext,
  type TokenManager,
  type TokenProvider,
} from './api/photobank/httpContext';