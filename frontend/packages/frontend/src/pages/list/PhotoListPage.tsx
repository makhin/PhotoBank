import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import type { FilterDto, PhotoItemDto } from '@photobank/shared/api/photobank';
import { useTranslation } from 'react-i18next';

import {
  usePhotoListAdapter,
  type UsePhotoListAdapterResult,
} from '@/features/photo/usePhotoListAdapter';
import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setFilter } from '@/features/photo/model/photoSlice';
import { open } from '@/features/viewer/viewerSlice';
import { readPhotoId, clearPhotoId } from '@/features/viewer/urlSync';
import EmptyState from '@/components/EmptyState';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';
import { Button } from '@/shared/ui/button';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { deserializeFilter } from '@/shared/lib/filter-url';
import { PhotoTable } from '@/features/photos/components/PhotoTable';
import { selectPersonsMap, selectTagsMap } from '@/features/metadata/selectors';

import PhotoListItemMobile from './PhotoListItemMobile';

const PhotoListPage = () => {
  const dispatch = useAppDispatch();
  const filter = useAppSelector((state) => state.photo.filter);
  const { t } = useTranslation();
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);
  const [searchParams] = useSearchParams();

  const {
    photos,
    counters,
    total,
    fetchNextPage,
    hasNextPage,
    isLoading,
    isFetchingNextPage,
  }: UsePhotoListAdapterResult = usePhotoListAdapter(filter);

  const {
    loaded: loadedCount,
    flags: { bw: bwCount, adult: adultCount, racy: racyCount },
  } = counters;
  const navigate = useNavigate();
  const location = useLocation();
  const scrollAreaRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);

  const [detailsId, setDetailsId] = useState<number | null>(null);
  const handleOpenDetails = useCallback((id: number) => {
    setDetailsId(id);
  }, []);
  const viewerItems = useMemo(
    () =>
      photos.map((p: PhotoItemDto) => ({
        id: p.id,
        preview: p.thumbnailUrl!,
        title: p.name,
      })),
    [photos]
  );
  const skeletonPhotos = useMemo(
    () =>
      Array.from({ length: 15 }, (_, i) => ({
        id: i,
        thumbnailUrl: '',
        name: '',
        storageName: '',
        relativePath: '',
      })) as PhotoItemDto[],
    []
  );
  const loading = isLoading && photos.length === 0;

  useEffect(() => {
    const encoded = searchParams.get('filter');
    if (encoded) {
      const parsed = deserializeFilter(encoded);
      if (parsed) {
        const urlFilter: FilterDto = {
          caption: parsed.caption,
          storages: parsed.storages?.map(Number),
          paths: parsed.paths?.map(Number),
          personNames: parsed.personNames,
          tagNames: parsed.tagNames,
          isBW: parsed.isBW,
          isAdultContent: parsed.isAdultContent,
          isRacyContent: parsed.isRacyContent,
          thisDay: parsed.thisDay ? { day: new Date().getDate(), month: new Date().getMonth() + 1 } : undefined,
          takenDateFrom: parsed.dateFrom ?? null,
          takenDateTo: parsed.dateTo ?? null,
          page: 1,
          pageSize: 10,
        };
        dispatch(setFilter(urlFilter));
      }
    }
  }, [searchParams, dispatch]);

  

  useEffect(() => {
    const id = readPhotoId(location.search);
    if (id && photos.length > 0) {
      const index = photos.findIndex((p: PhotoItemDto) => p.id === id);
      if (index >= 0) {
        dispatch(open({ items: viewerItems, index }));
      }
    }
  }, [dispatch, location.search, photos, viewerItems]);

  const viewerOpen = useAppSelector((s) => s.viewer.isOpen);
  const wasViewerOpen = useRef(false);
  useEffect(() => {
    if (wasViewerOpen.current && !viewerOpen) clearPhotoId();
    wasViewerOpen.current = viewerOpen;
  }, [viewerOpen]);

  const handleFilterOpen = useCallback(() => {
    navigate('/filter', { state: { useCurrentFilter: true } });
  }, [navigate]);

  useEffect(() => {
    const element = sentinelRef.current;
    const root = scrollAreaRef.current ?? undefined;
    if (!element || !root) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry && entry.isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage();
        }
      },
      { root }
    );
    observer.observe(element);
    return () => {
      observer.disconnect();
    };
  }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

  const handleDetailsOpenChange = useCallback((open: boolean) => {
    if (!open) setDetailsId(null);
  }, []);

  return (
    <div className="w-full h-screen flex flex-col bg-background">
      <div className="p-6 border-b flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('photoGalleryTitle')}</h1>
          <p className="text-muted-foreground mt-2 space-x-2 text-sm">
            <span>
              {loadedCount} of {total} photos
            </span>
            <span aria-hidden="true">•</span>
            <span>B/W: {bwCount}</span>
            <span aria-hidden="true">•</span>
            <span>NSFW: {adultCount}</span>
            <span aria-hidden="true">•</span>
            <span>Racy: {racyCount}</span>
          </p>
        </div>
        <Button variant="outline" onClick={handleFilterOpen}>
          {t('filterButtonText')}
        </Button>
      </div>

      <ScrollArea
        className="h-[calc(100vh-240px)]"
        ref={scrollAreaRef}
      >
        <div className="p-6">
          {/* Desktop/Tablet View */}
          <div className="hidden h-screen w-full overflow-hidden lg:block">
            <PhotoTable
              photos={photos}
              isFetchingNextPage={isFetchingNextPage}
              hasNextPage={hasNextPage}
              fetchNextPage={() => void fetchNextPage()}
              onRowClick={handleOpenDetails}
            />
          </div>

          {/* Mobile View */}
          <div className="block lg:hidden">
            <div className="grid gap-4 sm:grid-cols-2">
              {loading
                ? skeletonPhotos.slice(0, 6).map((p: PhotoItemDto) => (
                    <div
                      key={p.id}
                      className="h-40 w-full rounded-lg bg-muted animate-pulse"
                    />
                  ))
                : photos.length === 0 ? (
                    <EmptyState text="No photos" />
                  ) : (
                    photos.map((photo: PhotoItemDto) => (
                      <PhotoListItemMobile
                        key={photo.id}
                        photo={photo}
                        personsMap={personsMap}
                        tagsMap={tagsMap}
                        onClick={() => handleOpenDetails(photo.id)}
                      />
                    ))
                  )}
            </div>
          </div>
          {hasNextPage && (
            <div className="flex justify-center mt-4">
              <Button
                variant="outline"
                onClick={() => void fetchNextPage()}
                disabled={!hasNextPage || isFetchingNextPage}
              >
                {t('loadMoreButton')}
              </Button>
            </div>
          )}
          <div ref={sentinelRef} />
        </div>
      </ScrollArea>
      <PhotoDetailsModal
        photoId={detailsId}
        onOpenChange={handleDetailsOpenChange}
      />
    </div>
  );
};

export default PhotoListPage;

