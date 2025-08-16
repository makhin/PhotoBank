import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  photoGalleryTitle,
  filterButtonText,
  loadMoreButton,
} from '@photobank/shared/constants';

import { usePhotosSearchPhotos } from '@photobank/shared/api/photobank';
import { Button } from '@/shared/ui/button';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setLastResult } from '@/features/photo/model/photoSlice';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';
import { useViewer } from '@/features/viewer/state';
import { pushPhotoId, readPhotoId, clearPhotoId } from '@/features/viewer/urlSync';

import PhotoListItemMobile from './PhotoListItemMobile';
import VirtualPhotoList from './VirtualPhotoList';
import PhotoListItemSkeleton from './PhotoListItemSkeleton';
import EmptyState from '@/components/EmptyState';
import { photoColumns } from './columns';

const PhotoListPage = () => {
  const dispatch = useAppDispatch();
  const filter = useAppSelector((state) => state.photo.filter);
  const persons = useAppSelector((state) => state.metadata.persons);
  const tags = useAppSelector((state) => state.metadata.tags);

  const personsMap = useMemo(
    () => Object.fromEntries(persons.map((p) => [p.id, p.name])),
    [persons]
  );
  const tagsMap = useMemo(
    () => Object.fromEntries(tags.map((t) => [t.id, t.name])),
    [tags]
  );

  const { mutateAsync: searchPhotos, isPending: isLoading } = usePhotosSearchPhotos();
  type PhotosPage = { items?: PhotoItemDto[]; totalCount?: number };
  const [rawPhotos, setRawPhotos] = useState<PhotoItemDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(filter.page ?? 1);
  const pageSize = filter.pageSize ?? 10;
  const navigate = useNavigate();
  const location = useLocation();
  const scrollAreaRef = useRef<HTMLDivElement>(null);

  const [detailsId, setDetailsId] = useState<number | null>(null);
  const handleOpenDetails = useCallback((id: number) => {
    setDetailsId(id);
  }, []);
  const photos = useMemo(() => rawPhotos ?? [], [rawPhotos]);
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

  const renderPhotoRow = useCallback(
    (photo: PhotoItemDto) => {
      const handleClick = () => handleOpenDetails(photo.id);
      return (
        <div
          className="grid grid-cols-12 gap-2 px-4 py-2 cursor-pointer hover:bg-muted/50"
          onClick={handleClick}
        >
          {photoColumns.map((c) => (
            <div
              key={c.id}
              className={c.width}
              onClick={
                c.id === 'preview'
                  ? (e) => {
                      e.stopPropagation();
                      const index = photos.findIndex((p) => p.id === photo.id);
                      if (index >= 0) {
                        useViewer.getState().open(viewerItems, index);
                        pushPhotoId(photo.id);
                      }
                    }
                  : undefined
              }
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
    let cancelled = false;
    (async () => {
      try {
        const result = (await searchPhotos({ data: { ...filter, page: 1, pageSize } })) as { data: PhotosPage };
        if (cancelled) return;
        const fetched: PhotoItemDto[] = result.data.items ?? [];
        setRawPhotos(fetched);
        setTotal(result.data.totalCount ?? 0);
        setPage(1);
        dispatch(setLastResult(fetched));
      } catch {
        // ignore for now
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [searchPhotos, filter, dispatch, pageSize]);

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

  const loadMore = useCallback(async () => {
    const nextPage = page + 1;
    const result = (await searchPhotos({ data: { ...filter, page: nextPage, pageSize } })) as { data: PhotosPage };
    const newPhotos: PhotoItemDto[] = result.data.items ?? [];
    const updated = [...rawPhotos, ...newPhotos];
    setRawPhotos(updated);
    setPage(nextPage);
    setTotal(result.data.totalCount ?? 0);
    dispatch(setLastResult(updated));
  }, [searchPhotos, filter, page, rawPhotos, dispatch, pageSize]);

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
          {photos.length < total && (
            <div className="flex justify-center mt-4">
              <Button variant="outline" onClick={() => void loadMore()}>
                {loadMoreButton}
              </Button>
            </div>
          )}
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

