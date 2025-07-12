export const METADATA_CACHE_KEY = 'photobank_metadata_cache';
export const METADATA_CACHE_VERSION = 1;

export const MAX_VISIBLE_PERSONS_LG = 3;
export const MAX_VISIBLE_TAGS_LG = 3;

export const MAX_VISIBLE_PERSONS_SM = 2;
export const MAX_VISIBLE_TAGS_SM = 2;

export const PHOTOS_CACHE_KEY = 'photobank_photos_cache';
export const PHOTOS_CACHE_VERSION = 1;

export const DEFAULT_PHOTO_FILTER = {
  thisDay: true,
  skip: 0,
  top: 10,
} as const;

export const DEFAULT_FORM_VALUES = {
  caption: undefined,
  storages: [],
  paths: [],
  persons: [],
  tags: [],
  isBW: undefined,
  isAdultContent: undefined,
  isRacyContent: undefined,
  thisDay: undefined,
  dateFrom: undefined,
  dateTo: undefined,
} as const;
