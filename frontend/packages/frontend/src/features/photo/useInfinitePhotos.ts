import { useMemo } from 'react';
import { useInfiniteQuery } from '@tanstack/react-query';
import { photosSearchPhotos } from '@photobank/shared/api/photobank';
import type { FilterDto } from '@photobank/shared/api/photobank';

export const useInfinitePhotos = (filter: FilterDto) => {
  const pageSize = filter.pageSize ?? 10;

  const query = useInfiniteQuery({
    queryKey: ['photos', filter],
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      photosSearchPhotos({ ...filter, page: pageParam, pageSize }),
    getNextPageParam: (lastPage, pages) => {
      const total = lastPage.data.totalCount ?? 0;
      const loaded = pages.reduce(
        (sum, p) => sum + (p.data.items?.length ?? 0),
        0
      );
      return loaded < total ? pages.length + 1 : undefined;
    },
  });

  const items = useMemo(
    () => query.data?.pages.flatMap((p) => p.data.items ?? []) ?? [],
    [query.data]
  );

  const total = query.data?.pages[0]?.data.totalCount ?? 0;

  return { ...query, items, total };
};

export type UseInfinitePhotosResult = ReturnType<typeof useInfinitePhotos>;

