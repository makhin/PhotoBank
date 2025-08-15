import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  photoGalleryTitle,
  filterButtonText,
  loadMoreButton,
} from '@photobank/shared/constants';

import { useSearchPhotosMutation } from '@/shared/api.ts';
import { Button } from '@/shared/ui/button';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { useAppDispatch, useAppSelector } from '@/app/hook.ts';
import { setLastResult } from '@/features/photo/model/photoSlice.ts';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';

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

  const [searchPhotos, { isLoading }] = useSearchPhotosMutation();
  const [rawPhotos, setRawPhotos] = useState<PhotoItemDto[]>([]);
  const [total, setTotal] = useState(0);
  const [skip, setSkip] = useState(filter.skip ?? 0);
  const navigate = useNavigate();
  const scrollAreaRef = useRef<HTMLDivElement>(null);

  const [detailsId, setDetailsId] = useState<number | null>(null);
  const photos = useMemo(() => rawPhotos ?? [], [rawPhotos]);
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
            <div key={c.id} className={c.width}>
              {c.render(photo)}
            </div>
          ))}
        </div>
      );
    },
    [handleOpenDetails]
  );

  const renderSkeletonRow = useCallback(
    (_photo: PhotoItemDto) => <PhotoListItemSkeleton />,
    []
  );

  useEffect(() => {
    const promise = searchPhotos({ ...filter });
    (async () => {
      try {
        const result = await promise.unwrap();
        const fetched = result.photos || [];
        setRawPhotos(fetched);
        setTotal(result.count || 0);
        setSkip(fetched.length);
        dispatch(setLastResult(fetched));
      } catch {
        // ignore for now
      }
    })();
    return () => {
      promise.abort();
    };
  }, [searchPhotos, filter, dispatch]);

  const handleOpenDetails = useCallback((id: number) => {
    setDetailsId(id);
  }, []);

  const handleFilterOpen = useCallback(() => {
    navigate('/filter', { state: { useCurrentFilter: true } });
  }, [navigate]);

  const loadMore = useCallback(async () => {
    const result = await searchPhotos({ ...filter, skip }).unwrap();
    const newPhotos = result.photos || [];
    const updated = [...rawPhotos, ...newPhotos];
    const newSkip = skip + newPhotos.length;
    setRawPhotos(updated);
    setSkip(newSkip);
    setTotal(result.count || 0);
    dispatch(setLastResult(updated));
  }, [searchPhotos, filter, skip, rawPhotos, dispatch]);

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

