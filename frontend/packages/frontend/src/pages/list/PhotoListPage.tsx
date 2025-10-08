import { useMemo, useState, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { useTranslation } from 'react-i18next';

import {
  usePhotoListAdapter,
  type UsePhotoListAdapterResult,
} from '@/features/photo/usePhotoListAdapter';
import { useAppSelector } from '@/app/hook';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';
import { Button } from '@/shared/ui/button';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { selectPersonsMap, selectTagsMap } from '@/features/metadata/selectors';
import { useIntersectionObserver } from '@/hooks/useIntersectionObserver';

import PhotoListDesktop from './PhotoListDesktop';
import PhotoListMobile from './PhotoListMobile';
import { usePhotoListUrlSync } from './usePhotoListUrlSync';

const PhotoListPage = () => {
  const filter = useAppSelector((state) => state.photo.filter);
  const { t } = useTranslation();
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);

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

  usePhotoListUrlSync({ photos, viewerItems });

  const handleFilterOpen = useCallback(() => {
    navigate('/filter', { state: { useCurrentFilter: true } });
  }, [navigate]);

  const handleFetchNextPage = useCallback(() => {
    void fetchNextPage();
  }, [fetchNextPage]);

  const shouldObserve = hasNextPage && !isFetchingNextPage;

  useIntersectionObserver({
    target: sentinelRef,
    root: scrollAreaRef,
    onIntersect: handleFetchNextPage,
    enabled: shouldObserve,
  });

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
          <PhotoListDesktop
            photos={photos}
            hasNextPage={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            fetchNextPage={handleFetchNextPage}
            onRowClick={handleOpenDetails}
          />

          <PhotoListMobile
            loading={loading}
            photos={photos}
            skeletonPhotos={skeletonPhotos}
            personsMap={personsMap}
            tagsMap={tagsMap}
            onPhotoClick={handleOpenDetails}
          />
          {hasNextPage && (
            <div className="flex justify-center mt-4">
              <Button
                variant="outline"
                onClick={handleFetchNextPage}
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

