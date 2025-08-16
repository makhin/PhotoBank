import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import type { FilterDto, PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  photoGalleryTitle,
  filterButtonText,
  loadMoreButton,
} from '@photobank/shared/constants';

import { useInfinitePhotos } from '@/features/photo/useInfinitePhotos';
import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setFilter } from '@/features/photo/model/photoSlice';
import { useViewer } from '@/features/viewer/state';
import { pushPhotoId, readPhotoId, clearPhotoId } from '@/features/viewer/urlSync';
import EmptyState from '@/components/EmptyState';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';
import { Button } from '@/shared/ui/button';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { deserializeFilter } from '@/shared/lib/filter-url';

import PhotoListItemMobile from './PhotoListItemMobile';
import VirtualPhotoList from './VirtualPhotoList';
import PhotoListItemSkeleton from './PhotoListItemSkeleton';
import { photoColumns } from './columns';

const PhotoListPage = () => {
  const dispatch = useAppDispatch();
  const filter = useAppSelector((state) => state.photo.filter);
  const persons = useAppSelector((state) => state.metadata.persons);
  const tags = useAppSelector((state) => state.metadata.tags);
  const [searchParams] = useSearchParams();

  const personsMap = useMemo(
    () => Object.fromEntries(persons.map((p) => [p.id, p.name])),
    [persons]
  );
  const tagsMap = useMemo(
    () => Object.fromEntries(tags.map((t) => [t.id, t.name])),
    [tags]
  );

  const {
    items: photos,
    total,
    fetchNextPage,
    hasNextPage,
    isLoading,
    isFetchingNextPage,
  } = useInfinitePhotos(filter);
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
      photos.map((p) => ({
        id: p.id,
        preview: p.previewUrl!,
        original: p.originalUrl!,
        title: p.name,
      })),
    [photos]
  );
  const skeletonPhotos = useMemo(
    () =>
      Array.from({ length: 15 }, (_, i) => ({
        id: i,
        thumbnail: '',
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
          persons: parsed.persons?.map(Number),
          tags: parsed.tags?.map(Number),
          isBW: parsed.isBW,
          isAdultContent: parsed.isAdultContent,
          isRacyContent: parsed.isRacyContent,
          thisDay: parsed.thisDay,
          takenDateFrom: parsed.dateFrom?.toISOString(),
          takenDateTo: parsed.dateTo?.toISOString(),
          page: 1,
          pageSize: 10,
        };
        dispatch(setFilter(urlFilter));
      }
    }
  }, [searchParams, dispatch]);

  const renderPhotoRow = useCallback(
    (photo: PhotoItemDto) => {
      const handleClick = () => handleOpenDetails(photo.id);
        return (
          <div
            role="button"
            tabIndex={0}
            className="grid grid-cols-12 gap-2 px-4 py-2 cursor-pointer hover:bg-muted/50"
            onClick={handleClick}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                handleClick();
              }
            }}
          >
            {photoColumns.map((c) => (
              <div
                key={c.id}
                className={c.width}
                {...(c.id === 'preview'
                  ? {
                      role: 'button',
                      tabIndex: 0,
                      onClick: (e: React.MouseEvent) => {
                        e.stopPropagation();
                        const index = photos.findIndex((p) => p.id === photo.id);
                        if (index >= 0) {
                          useViewer.getState().open(viewerItems, index);
                          pushPhotoId(photo.id);
                        }
                      },
                      onKeyDown: (e: React.KeyboardEvent) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.stopPropagation();
                          const index = photos.findIndex((p) => p.id === photo.id);
                          if (index >= 0) {
                            useViewer.getState().open(viewerItems, index);
                            pushPhotoId(photo.id);
                          }
                        }
                      },
                    }
                  : {})}
              >
                {c.render(photo)}
              </div>
            ))}
          </div>
        );
      },
      [handleOpenDetails, photos, viewerItems]
    );

  const renderSkeletonRow = useCallback(
    (_photo: PhotoItemDto) => <PhotoListItemSkeleton />,
    []
  );

  useEffect(() => {
    const id = readPhotoId(location.search);
    if (id && photos.length > 0) {
      const index = photos.findIndex((p) => p.id === id);
      if (index >= 0) {
        useViewer.getState().open(viewerItems, index);
      }
    }
  }, [location.search, photos, viewerItems]);

  useEffect(() => {
    const unsubscribe = useViewer.subscribe((s) => {
      if (!s.isOpen) clearPhotoId();
    });
    return unsubscribe;
  }, []);

  const handleFilterOpen = useCallback(() => {
    navigate('/filter', { state: { useCurrentFilter: true } });
  }, [navigate]);

  useEffect(() => {
    const element = sentinelRef.current;
    const root = scrollAreaRef.current ?? undefined;
    if (!element || !root) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && hasNextPage && !isFetchingNextPage) {
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
          <h1 className="text-3xl font-bold">{photoGalleryTitle}</h1>
          <p className="text-muted-foreground mt-2">
            {photos.length} of {total} photos
          </p>
        </div>
        <Button variant="outline" onClick={handleFilterOpen}>
          {filterButtonText}
        </Button>
      </div>

      <ScrollArea className="h-[calc(100vh-240px)]" ref={scrollAreaRef}>
        <div className="p-6">
          {/* Desktop/Tablet View */}
          <div className="hidden lg:block">
            <div className="grid grid-cols-12 gap-4 mb-4 px-4 py-2 bg-muted/50 rounded-lg font-medium text-sm">
              {photoColumns.map((c) => (
                <div key={c.id} className={c.width}>
                  {c.header}
                </div>
              ))}
            </div>
            {loading ? (
              <VirtualPhotoList
                photos={skeletonPhotos}
                parentRef={scrollAreaRef}
                renderRow={renderSkeletonRow}
              />
            ) : photos.length === 0 ? (
              <EmptyState text="No photos" />
            ) : (
              <VirtualPhotoList
                photos={photos}
                parentRef={scrollAreaRef}
                renderRow={renderPhotoRow}
              />
            )}
          </div>

          {/* Mobile View */}
          <div className="lg:hidden">
            <div className="grid gap-4 sm:grid-cols-2">
              {loading
                ? skeletonPhotos.slice(0, 6).map((p) => (
                    <PhotoListItemSkeleton key={p.id} />
                  ))
                : photos.length === 0 ? (
                    <EmptyState text="No photos" />
                  ) : (
                    photos.map((photo) => (
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
                {loadMoreButton}
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

