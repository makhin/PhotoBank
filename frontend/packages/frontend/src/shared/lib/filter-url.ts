import type { FilterDto } from '@photobank/shared/api/photobank';
import { formSchema, type FormData } from '@/features/filter/lib/form-schema';

const encode = (data: FormData): string => {
  const json = JSON.stringify(data);
  if (typeof window === 'undefined') {
    return Buffer.from(json, 'utf-8').toString('base64');
  }
  return btoa(json);
};

const decode = (value: string): FormData | null => {
  try {
    const json =
      typeof window === 'undefined'
        ? Buffer.from(value, 'base64').toString('utf-8')
        : atob(value);
    const parsed = JSON.parse(json);
    return formSchema.parse(parsed);
  } catch {
    return null;
  }
};

export const serializeFilter = (data: FormData): string => encode(data);

export const deserializeFilter = (value: string): FilterDto | null => {
  const form = decode(value);
  return form ? formDataToFilterDto(form) : null;
};

export const formDataToFilterDto = (data: FormData): FilterDto => ({
  caption: data.caption,
  storages: data.storages?.map(Number),
  paths: data.paths?.map(Number),
  persons: data.persons?.map(Number),
  tags: data.tags?.map(Number),
  isBW: data.isBW,
  isAdultContent: data.isAdultContent,
  isRacyContent: data.isRacyContent,
  thisDay: data.thisDay,
  takenDateFrom: data.dateFrom?.toISOString(),
  takenDateTo: data.dateTo?.toISOString(),
  page: 1,
  pageSize: 10,
});

export const filterDtoToFormData = (filter: FilterDto): FormData => ({
  caption: filter.caption ?? undefined,
  storages: filter.storages?.map(String) ?? [],
  paths: filter.paths?.map(String) ?? [],
  persons: filter.persons?.map(String) ?? [],
  tags: filter.tags?.map(String) ?? [],
  isBW: filter.isBW ?? undefined,
  isAdultContent: filter.isAdultContent ?? undefined,
  isRacyContent: filter.isRacyContent ?? undefined,
  thisDay: filter.thisDay ?? undefined,
  dateFrom: filter.takenDateFrom ? new Date(filter.takenDateFrom) : undefined,
  dateTo: filter.takenDateTo ? new Date(filter.takenDateTo) : undefined,
});

