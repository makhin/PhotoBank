import { renderHook } from '@testing-library/react';
import { vi, beforeEach, describe, expect, test, type Mock } from 'vitest';
import type {
  FilterDto,
  PhotoItemDto,
} from '@photobank/shared/api/photobank';

import { usePhotoListAdapter } from './usePhotoListAdapter';
import { useInfinitePhotos } from './useInfinitePhotos';

vi.mock('./useInfinitePhotos');

describe('usePhotoListAdapter', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  test('exposes photos and derived counters', () => {
    const photos: PhotoItemDto[] = [
      {
        id: 1,
        name: 'first',
        storageName: 's',
        relativePath: 'a',
        isBW: true,
        isAdultContent: true,
      },
      {
        id: 2,
        name: 'second',
        storageName: 's',
        relativePath: 'b',
        isRacyContent: true,
      },
      {
        id: 3,
        name: 'third',
        storageName: 's',
        relativePath: 'c',
      },
    ];

    const fetchNextPage = vi.fn();

    (useInfinitePhotos as unknown as Mock).mockReturnValue({
      items: photos,
      total: 10,
      fetchNextPage,
      hasNextPage: true,
      isLoading: false,
      isFetchingNextPage: false,
    });

    const { result } = renderHook(() =>
      usePhotoListAdapter({} as FilterDto)
    );

    expect(result.current.photos).toEqual(photos);
    expect(result.current.counters).toEqual({
      total: 10,
      loaded: photos.length,
      flags: { bw: 1, adult: 1, racy: 1 },
    });
    expect(result.current.fetchNextPage).toBe(fetchNextPage);
  });
});
