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
export {
  cachePhotoItem,
  getCachedPhotoItem,
  cachePhoto,
  getCachedPhoto,
} from './cache/photosCache';
export {
  cacheFilterResult,
  getCachedFilterResult,
} from './cache/filterResultsCache';
