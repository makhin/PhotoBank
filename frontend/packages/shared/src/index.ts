import {format, parseISO} from "date-fns";

export const formatDate = (dateString?: string) => {
  if (!dateString) return 'не указана дата';
  try {
    return format(parseISO(dateString), 'dd.MM.yyyy, HH:mm');
  } catch {
    return 'неверный формат даты';
  }
};

export const getGenderText = (gender?: boolean) => {
  if (gender === undefined) return 'не указан пол';
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

export { checkIsAdmin } from './utils/admin';
export { useIsAdmin } from './hooks/useIsAdmin';
export { getPlaceByGeoPoint } from './utils/geocode';
