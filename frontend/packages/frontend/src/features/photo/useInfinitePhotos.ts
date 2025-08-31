import { useMemo } from 'react';
import { useInfiniteQuery, type InfiniteData } from '@tanstack/react-query';
import { photosSearchPhotos } from '@photobank/shared/api/photobank';
import type {
  FilterDto,
  PhotoItemDto,
  photosSearchPhotosResponse,
} from '@photobank/shared/api/photobank';

export const useInfinitePhotos = (filter: FilterDto) => {
  const pageSize = filter.pageSize ?? 10;

  const query = useInfiniteQuery<
    photosSearchPhotosResponse,
    Error,
    InfiniteData<photosSearchPhotosResponse>,
    [string, FilterDto],
    number
  >({
    queryKey: ['photos', filter],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      photosSearchPhotos({
        ...filter,
        thisDay: filter.thisDay ?? undefined,
        page: pageParam,
        pageSize,
      }),
    getNextPageParam: (
      lastPage: photosSearchPhotosResponse,
      pages: photosSearchPhotosResponse[]
    ) => {
      const total = lastPage.status === 200 ? lastPage.data.totalCount ?? 0 : 0;
      const loaded = pages.reduce((sum, p: photosSearchPhotosResponse) => {
        if (p.status === 200) {
          return sum + (p.data.items?.length ?? 0);
        }
        return sum;
      }, 0);
      return loaded < total ? pages.length + 1 : undefined;
    },
  });

  const items: PhotoItemDto[] = useMemo(
    () =>
      query.data?.pages.flatMap((p: photosSearchPhotosResponse) =>
        p.status === 200 ? p.data.items ?? [] : []
      ) ?? [],
    [query.data]
  );

  const total =
    query.data?.pages[0]?.status === 200
      ? query.data.pages[0].data.totalCount ?? 0
      : 0;

  return { ...query, items, total };
};

export type UseInfinitePhotosResult = ReturnType<typeof useInfinitePhotos>;

