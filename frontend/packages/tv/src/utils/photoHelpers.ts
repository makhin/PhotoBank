// src/utils/photoHelpers.ts
import { format } from 'date-fns';
import type { PhotoItemDto, PersonDto, TagDto } from '@/api';

/**
 * Расширенный тип PhotoItem с преобразованными именами
 */
export interface PhotoItemDisplay extends PhotoItemDto {
  personNames?: string[];
  tagNames?: string[];
}

/**
 * Преобразует массив PhotoItemDto в PhotoItemDisplay с именами персон и тегов
 */
export const mapPhotosToDisplay = (
  photos: PhotoItemDto[],
  persons: PersonDto[],
  tags: TagDto[]
): PhotoItemDisplay[] => {
  // Создаём мапы для быстрого поиска
  const personsMap = new Map(persons.map((p) => [p.id, p.name]));
  const tagsMap = new Map(tags.map((t) => [t.id, t.name]));

  return photos.map((photo) => ({
    ...photo,
    personNames:
      photo.persons?.map((personId: number) => personsMap.get(personId) || 'Unknown') || [],
    tagNames: photo.tags?.map((tagId: number) => tagsMap.get(tagId) || 'Unknown') || [],
  }));
};

/**
 * Форматирует дату в формат dd.MM.yyyy HH:mm
 */
export const formatPhotoDate = (date?: Date | null): string => {
  if (!date) return '';

  try {
    return format(date, 'dd.MM.yyyy HH:mm');
  } catch (error) {
    console.error('Error formatting date:', error);
    return '';
  }
};

/**
 * Получает первую caption или возвращает пустую строку
 */
export const getMainCaption = (captions?: string[] | null): string | undefined => {
  return captions && captions.length > 0 ? captions[0] : undefined;
};

/**
 * Проверяет является ли контент NSFW
 */
export const isNSFW = (photo: PhotoItemDto): boolean => {
  return photo.isAdultContent === true || (photo.adultScore ?? 0) > 0.7;
};

/**
 * Генерирует текст для отображения людей
 */
export const getPersonsDisplayText = (personNames?: string[]): string | undefined => {
  if (!personNames || personNames.length === 0) return undefined;

  if (personNames.length === 1) return personNames[0];
  if (personNames.length === 2) return personNames.join(' и ');

  return `${personNames.slice(0, 2).join(', ')} и ещё ${personNames.length - 2}`;
};

/**
 * Генерирует текст для отображения тегов
 */
export const getTagsDisplayText = (tagNames?: string[], maxLength = 50): string => {
  if (!tagNames || tagNames.length === 0) return '';

  const joined = tagNames.join(', ');

  if (joined.length <= maxLength) return joined;

  // Обрезаем до maxLength и добавляем многоточие
  return joined.substring(0, maxLength - 3) + '...';
};

/**
 * Фильтрует фото по различным критериям
 */
export const filterPhotos = (
  photos: PhotoItemDisplay[],
  filters: {
    hideNSFW?: boolean;
    storageId?: number;
    personName?: string;
    tagName?: string;
    dateFrom?: Date;
    dateTo?: Date;
  }
): PhotoItemDisplay[] => {
  let filtered = [...photos];

  // Фильтр NSFW
  if (filters.hideNSFW) {
    filtered = filtered.filter((photo) => !isNSFW(photo));
  }

  // Фильтр по storageId (в реальности нужно будет мапить storageName -> id)
  if (filters.storageId !== undefined) {
    // В текущей схеме нет storageId, только storageName
    // Оставляем для будущей реализации
  }

  // Фильтр по имени персоны
  if (filters.personName) {
    filtered = filtered.filter((photo) =>
      photo.personNames?.some((name) =>
        name.toLowerCase().includes(filters.personName!.toLowerCase())
      )
    );
  }

  // Фильтр по тегу
  if (filters.tagName) {
    filtered = filtered.filter((photo) =>
      photo.tagNames?.some((tag) => tag.toLowerCase().includes(filters.tagName!.toLowerCase()))
    );
  }

  // Фильтр по дате
  if (filters.dateFrom || filters.dateTo) {
    filtered = filtered.filter((photo) => {
      const takenDate = photo.takenDate;
      if (!takenDate) return false;

      // takenDate уже Date объект
      if (filters.dateFrom && takenDate < filters.dateFrom) return false;
      if (filters.dateTo && takenDate > filters.dateTo) return false;

      return true;
    });
  }

  return filtered;
};

/**
 * Группирует фото по дате (для timeline view)
 */
export const groupPhotosByDate = (photos: PhotoItemDto[]): Map<string, PhotoItemDto[]> => {
  const grouped = new Map<string, PhotoItemDto[]>();

  photos.forEach((photo) => {
    const takenDate = photo.takenDate;
    if (!takenDate) {
      const key = 'Без даты';
      const existing = grouped.get(key) || [];
      grouped.set(key, [...existing, photo]);
      return;
    }

    // takenDate уже Date объект
    const key = `${takenDate.getFullYear()}-${String(takenDate.getMonth() + 1).padStart(2, '0')}-${String(
      takenDate.getDate()
    ).padStart(2, '0')}`;

    const existing = grouped.get(key) || [];
    grouped.set(key, [...existing, photo]);
  });

  return grouped;
};

/**
 * Сортирует фото по различным критериям
 */
export const sortPhotos = (
  photos: PhotoItemDto[],
  sortBy: 'date' | 'name' | 'storage',
  order: 'asc' | 'desc' = 'desc'
): PhotoItemDto[] => {
  const sorted = [...photos];

  sorted.sort((a, b) => {
    let comparison = 0;

    switch (sortBy) {
      case 'date': {
        // takenDate уже Date объект
        const dateA = a.takenDate ? a.takenDate.getTime() : 0;
        const dateB = b.takenDate ? b.takenDate.getTime() : 0;
        comparison = dateA - dateB;
        break;
      }

      case 'name':
        comparison = a.name.localeCompare(b.name);
        break;

      case 'storage':
        comparison = a.storageName.localeCompare(b.storageName);
        break;
    }

    return order === 'asc' ? comparison : -comparison;
  });

  return sorted;
};
