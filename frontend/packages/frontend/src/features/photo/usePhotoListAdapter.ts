import { useMemo } from 'react';
import type {
  FilterDto,
  PhotoItemDto,
} from '@photobank/shared/api/photobank';

import {
  useInfinitePhotos,
  type UseInfinitePhotosResult,
} from './useInfinitePhotos';

export type PhotoFlagCounters = {
  bw: number;
  adult: number;
  racy: number;
};

export type PhotoListCounters = {
  total: number;
  loaded: number;
  flags: PhotoFlagCounters;
};

export type UsePhotoListAdapterResult = Omit<UseInfinitePhotosResult, 'items'> & {
  photos: PhotoItemDto[];
  counters: PhotoListCounters;
};

const initialFlagCounters: PhotoFlagCounters = Object.freeze({
  bw: 0,
  adult: 0,
  racy: 0,
});

function countFlags(photos: PhotoItemDto[]): PhotoFlagCounters {
  return photos.reduce<PhotoFlagCounters>((acc, photo) => {
    if (photo.isBW) acc.bw += 1;
    if (photo.isAdultContent) acc.adult += 1;
    if (photo.isRacyContent) acc.racy += 1;
    return acc;
  }, { ...initialFlagCounters });
}

export const usePhotoListAdapter = (
  filter: FilterDto
): UsePhotoListAdapterResult => {
  const { items, total, ...query } = useInfinitePhotos(filter);

  const counters = useMemo<PhotoListCounters>(() => {
    const flags = countFlags(items);

    return {
      total,
      loaded: items.length,
      flags,
    };
  }, [items, total]);

  return {
    ...query,
    total,
    photos: items,
    counters,
  };
};
