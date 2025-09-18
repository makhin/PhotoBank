// packages/shared/src/index.ts

type FlexibleDateInput = string | number | Date | null | undefined;

const ruDateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

export const formatDate = (dateInput?: FlexibleDateInput) => {
  if (dateInput === null || dateInput === undefined) return 'не указана дата';
  if (typeof dateInput === 'string' && dateInput.length === 0) {
    return 'не указана дата';
  }

  let date: Date;

  if (dateInput instanceof Date) {
    date = dateInput;
  } else if (typeof dateInput === 'number') {
    date = new Date(dateInput);
  } else if (typeof dateInput === 'string') {
    date = new Date(dateInput);
  } else {
    return 'неверный формат даты';
  }

  if (Number.isNaN(date.getTime())) {
    return 'неверный формат даты';
  }

  return ruDateTimeFormatter.format(date);
};

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
export { uploadPhotosAdapter } from './adapters/photos-upload.adapter';

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