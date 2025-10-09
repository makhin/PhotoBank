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

type MockedInfiniteQueryOptions = {
  getNextPageParam?: (
    lastPage: photosSearchPhotosResponse,
    pages: photosSearchPhotosResponse[]
  ) => number | undefined;
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
    let capturedOptions: MockedInfiniteQueryOptions | null = null;

    const firstPage = createPage([1, 2, 3], 10);

    useInfiniteQueryMock.mockImplementation((options: MockedInfiniteQueryOptions) => {
      capturedOptions = options;
      return { data: { pages: [firstPage] } };
    });

    renderHook(() => useInfinitePhotos({ pageSize: 3 } as FilterDto));

    expect(capturedOptions).not.toBeNull();
    const nextPage = capturedOptions?.getNextPageParam?.(firstPage, [firstPage]);
    expect(nextPage).toBe(2);
  });

  test('stops pagination once all items are loaded', () => {
    let capturedOptions: MockedInfiniteQueryOptions | null = null;

    const firstPage = createPage([1, 2, 3], 6);
    const secondPage = createPage([4, 5, 6], 6);

    useInfiniteQueryMock.mockImplementation((options: MockedInfiniteQueryOptions) => {
      capturedOptions = options;
      return { data: { pages: [firstPage, secondPage] } };
    });

    const { result } = renderHook(() => useInfinitePhotos({} as FilterDto));

    const combinedItems = [
      ...(firstPage.data.items ?? []),
      ...(secondPage.data.items ?? []),
    ];
    expect(result.current.items).toEqual(combinedItems);
    expect(result.current.total).toBe(6);

    expect(capturedOptions).not.toBeNull();
    const nextPage = capturedOptions?.getNextPageParam?.(secondPage, [
      firstPage,
      secondPage,
    ]);
    expect(nextPage).toBeUndefined();
  });
});
