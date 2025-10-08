import { useEffect, useRef } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import type { FilterDto, PhotoItemDto } from '@photobank/shared/api/photobank';

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

    const urlFilter: FilterDto = {
      caption: parsed.caption,
      storages: parsed.storages?.map(Number),
      paths: parsed.paths?.map(Number),
      personNames: parsed.personNames,
      tagNames: parsed.tagNames,
      isBW: parsed.isBW,
      isAdultContent: parsed.isAdultContent,
      isRacyContent: parsed.isRacyContent,
      thisDay: parsed.thisDay
        ? { day: new Date().getDate(), month: new Date().getMonth() + 1 }
        : undefined,
      takenDateFrom: parsed.dateFrom ?? null,
      takenDateTo: parsed.dateTo ?? null,
      page: 1,
      pageSize: 10,
    };

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
