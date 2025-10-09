import type { FilterDto } from '../api/photobank';

const DEFAULT_PAGE = 1;
const DEFAULT_PAGE_SIZE = 10;

const toThisDay = (now: Date): NonNullable<FilterDto['thisDay']> => ({
  day: now.getDate(),
  month: now.getMonth() + 1,
});

export const withPagination = (
  filter: Partial<FilterDto>,
  page?: number,
): FilterDto => {
  const nextPage = page ?? filter.page ?? DEFAULT_PAGE;
  const nextPageSize = filter.pageSize ?? DEFAULT_PAGE_SIZE;
  const { page: _ignoredPage, pageSize: _ignoredPageSize, ...rest } = filter;

  return {
    ...rest,
    page: nextPage,
    pageSize: nextPageSize,
  };
};

export const createThisDayFilter = (now: Date): FilterDto =>
  withPagination({ thisDay: toThisDay(now) });

export const DEFAULT_PHOTO_FILTER: FilterDto = createThisDayFilter(new Date());
