import { useEffect, useRef } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import type { FilterDto, PhotoItemDto } from '@photobank/shared/api/photobank';
import { createThisDayFilter, withPagination } from '@photobank/shared';

import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setFilter } from '@/features/photo/model/photoSlice';
import type { ViewerItem } from '@/features/viewer/viewerSlice';
import { clearPhotoId, readPhotoId } from '@/features/viewer/urlSync';
import { open } from '@/features/viewer/viewerSlice';
import { deserializeFilter } from '@/shared/lib/filter-url';

interface UsePhotoListUrlSyncParams {
  photos: PhotoItemDto[];
  viewerItems: ViewerItem[];
}

export const usePhotoListUrlSync = ({
  photos,
  viewerItems,
}: UsePhotoListUrlSyncParams) => {
  const dispatch = useAppDispatch();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const viewerOpen = useAppSelector((state) => state.viewer.isOpen);
  const wasViewerOpen = useRef(false);

  useEffect(() => {
    const encoded = searchParams.get('filter');
    if (!encoded) return;

    const parsed = deserializeFilter(encoded);
    if (!parsed) return;

    const urlFilter: FilterDto = withPagination({
      caption: parsed.caption,
      storages: parsed.storages?.map(Number),
      paths: parsed.paths?.map(Number),
      personNames: parsed.personNames,
      tagNames: parsed.tagNames,
      isBW: parsed.isBW,
      isAdultContent: parsed.isAdultContent,
      isRacyContent: parsed.isRacyContent,
      thisDay: parsed.thisDay
        ? createThisDayFilter(new Date()).thisDay
        : undefined,
      takenDateFrom: parsed.dateFrom ?? null,
      takenDateTo: parsed.dateTo ?? null,
    });

    dispatch(setFilter(urlFilter));
  }, [dispatch, searchParams]);

  useEffect(() => {
    const id = readPhotoId(location.search);
    if (!id || photos.length === 0) return;

    const index = photos.findIndex((photo: PhotoItemDto) => photo.id === id);
    if (index < 0) return;

    dispatch(open({ items: viewerItems, index }));
  }, [dispatch, location.search, photos, viewerItems]);

  useEffect(() => {
    if (wasViewerOpen.current && !viewerOpen) {
      clearPhotoId();
    }
    wasViewerOpen.current = viewerOpen;
  }, [viewerOpen]);
};
