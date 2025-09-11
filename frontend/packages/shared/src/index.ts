import {format, parseISO} from "date-fns";

export const formatDate = (dateString?: string) => {
  if (!dateString) return 'не указана дата';
  try {
    return format(parseISO(dateString), 'dd.MM.yyyy, HH:mm');
  } catch {
    return 'неверный формат даты';
  }
};

export const getGenderText = (gender?: boolean | null) => {
  if (gender === undefined || gender === null) return 'не указан пол';
  return gender ? 'Муж' : 'Жен';
};

export { getFilterHash } from './utils/getFilterHash.js';
export { getOrientation } from './utils/getOrientation.js';
export {
  cachePhoto,
  getCachedPhoto,
} from './cache/photosCache.js';
export {
  cacheFilterResult,
  getCachedFilterResult,
} from './cache/filterResultsCache.js';

export const firstNWords = (sentence: string, count: number): string => {
  const trimmed = sentence.trim();
  if (trimmed === '') return '';

  const words = trimmed.split(/\s+/);
  if (words.length <= count) {
    return trimmed + ' ';
  }

  return words.slice(0, count).join(' ') + '... ';
};

export { useIsAdmin } from './hooks/useIsAdmin.js';
export { useCanSeeNsfw } from './hooks/useCanSeeNsfw.js';
export { getPlaceByGeoPoint } from './utils/geocode.js';
export { uploadPhotosAdapter } from './adapters/photos-upload.adapter.js';
