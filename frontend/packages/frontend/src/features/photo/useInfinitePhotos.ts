import { useMemo } from 'react';
import { useInfiniteQuery } from '@tanstack/react-query';
import { photosSearchPhotos } from '@photobank/shared/api/photobank';
import type {
  FilterDto,
  photosSearchPhotosResponse,
  PhotoItemDto,
} from '@photobank/shared/api/photobank';

export const useInfinitePhotos = (filter: FilterDto) => {
  const pageSize = filter.pageSize ?? 10;

  const query = useInfiniteQuery<photosSearchPhotosResponse>({
    queryKey: ['photos', filter],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      photosSearchPhotos({
        ...filter,
        thisDay: filter.thisDay ?? undefined,
        page: pageParam as number,
        pageSize,
      }),
    getNextPageParam: (lastPage, pages) => {
      const total = lastPage.status === 200 ? lastPage.data.totalCount ?? 0 : 0;
      const loaded = pages.reduce((sum, p) => {
        if (p.status === 200) {
          return sum + (p.data.items?.length ?? 0);
        }
        return sum;
      }, 0);
      return loaded < total ? pages.length + 1 : undefined;
    },
  });

  const items = useMemo(
    () =>
      (query.data?.pages.flatMap((p) =>
        p.status === 200 ? p.data.items ?? [] : []
      ) ?? []) as PhotoItemDto[],
    [query.data]
  );

  const total =
    query.data?.pages[0]?.status === 200
      ? query.data.pages[0].data.totalCount ?? 0
      : 0;

  return { ...query, items, total };
};

export type UseInfinitePhotosResult = ReturnType<typeof useInfinitePhotos>;

