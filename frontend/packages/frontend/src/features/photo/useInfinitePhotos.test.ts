import { renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, test, vi, type Mock } from 'vitest';
import type {
  FilterDto,
  PhotoItemDto,
  photosSearchPhotosResponse,
} from '@photobank/shared/api/photobank';

import { useInfinitePhotos } from './useInfinitePhotos';
import { useInfiniteQuery } from '@tanstack/react-query';

vi.mock('@tanstack/react-query', () => ({
  useInfiniteQuery: vi.fn(),
}));

const useInfiniteQueryMock = useInfiniteQuery as unknown as Mock;

type GetNextPageParam = (
  lastPage: photosSearchPhotosResponse,
  pages: photosSearchPhotosResponse[]
) => number | undefined;

type MockedInfiniteQueryOptions = {
  getNextPageParam?: GetNextPageParam;
};

const createPhoto = (id: number): PhotoItemDto => ({
  id,
  name: `photo-${id}`,
  storageName: 'storage',
  relativePath: `path/${id}`,
});

const createPage = (
  ids: number[],
  totalCount: number
): photosSearchPhotosResponse => ({
  status: 200,
  headers: new Headers(),
  data: {
    totalCount,
    items: ids.map(createPhoto),
  },
});

describe('useInfinitePhotos domain adapter', () => {
  beforeEach(() => {
    useInfiniteQueryMock.mockReset();
  });

  test('requests another page while more items remain', () => {
    let capturedGetNextPageParam: GetNextPageParam | undefined;

    const firstPage = createPage([1, 2, 3], 10);

    useInfiniteQueryMock.mockImplementation((options: MockedInfiniteQueryOptions) => {
      capturedGetNextPageParam = options?.getNextPageParam;
      return { data: { pages: [firstPage] } };
    });

    renderHook(() => useInfinitePhotos({ pageSize: 3 } as FilterDto));

    expect(capturedGetNextPageParam).toBeDefined();
    const nextPage = capturedGetNextPageParam?.(firstPage, [firstPage]);
    expect(nextPage).toBe(2);
  });

  test('stops pagination once all items are loaded', () => {
    let capturedGetNextPageParam: GetNextPageParam | undefined;

    const firstPage = createPage([1, 2, 3], 6);
    const secondPage = createPage([4, 5, 6], 6);

    useInfiniteQueryMock.mockImplementation((options: MockedInfiniteQueryOptions) => {
      capturedGetNextPageParam = options?.getNextPageParam;
      return { data: { pages: [firstPage, secondPage] } };
    });

    const { result } = renderHook(() => useInfinitePhotos({} as FilterDto));

    const firstItems = Array.isArray(firstPage.data.items) ? firstPage.data.items : [];
    const secondItems = Array.isArray(secondPage.data.items) ? secondPage.data.items : [];
    const combinedItems = [...firstItems, ...secondItems];
    expect(result.current.items).toEqual(combinedItems);
    expect(result.current.total).toBe(6);

    expect(capturedGetNextPageParam).toBeDefined();
    const nextPage = capturedGetNextPageParam?.(secondPage, [
      firstPage,
      secondPage,
    ]);
    expect(nextPage).toBeUndefined();
  });
});
