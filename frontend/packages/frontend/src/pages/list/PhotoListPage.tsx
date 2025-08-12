import { useSelector } from 'react-redux';
import { useEffect, useMemo, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import { useSearchPhotosMutation } from '@/shared/api.ts';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { RootState } from '@/app/store.ts';
import { useAppDispatch } from '@/app/hook.ts';
import { setLastResult } from '@/features/photo/model/photoSlice.ts';
import {
  photoGalleryTitle,
  filterButtonText,
  loadMoreButton,
  colIdLabel,
  colPreviewLabel,
  colNameLabel,
  colDateLabel,
  colStorageLabel,
  colFlagsLabel,
  colDetailsLabel,
} from '@photobank/shared/constants';
import PhotoDetailsModal from '@/components/PhotoDetailsModal';
import PhotoListItemDesktop from './PhotoListItemDesktop';
import PhotoListItemMobile from './PhotoListItemMobile';

const PhotoListPage = () => {
  const dispatch = useAppDispatch();
  const filter = useSelector((state: RootState) => state.photo.filter);
  const persons = useSelector((state: RootState) => state.metadata.persons);
  const tags = useSelector((state: RootState) => state.metadata.tags);

  const personsMap = useMemo(
    () => Object.fromEntries(persons.map((p) => [p.id, p.name])),
    [persons]
  );
  const tagsMap = useMemo(
    () => Object.fromEntries(tags.map((t) => [t.id, t.name])),
    [tags]
  );

  const [searchPhotos] = useSearchPhotosMutation();
  const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
  const [total, setTotal] = useState(0);
  const [skip, setSkip] = useState(filter.skip ?? 0);
  const navigate = useNavigate();
  const scrollAreaRef = useRef<HTMLDivElement>(null);

  const [detailsId, setDetailsId] = useState<number | null>(null);

  useEffect(() => {
    searchPhotos({ ...filter })
      .unwrap()
      .then((result) => {
        const fetched = result.photos || [];
        setPhotos(fetched);
        setTotal(result.count || 0);
        setSkip(fetched.length);
        dispatch(setLastResult(fetched));
      });
  }, [searchPhotos, filter, dispatch]);

  const loadMore = () => {
    searchPhotos({ ...filter, skip })
      .unwrap()
      .then((result) => {
        const newPhotos = result.photos || [];
        const updated = [...photos, ...newPhotos];
        const newSkip = skip + newPhotos.length;
        setPhotos(updated);
        setSkip(newSkip);
        setTotal(result.count || 0);
        dispatch(setLastResult(updated));
      });
  };

  return (
    <div className="w-full h-screen flex flex-col bg-background">
      <div className="p-6 border-b flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{photoGalleryTitle}</h1>
          <p className="text-muted-foreground mt-2">
            {photos.length} of {total} photos
          </p>
        </div>
        <Button
          variant="outline"
          onClick={() => {
            navigate('/filter', { state: { useCurrentFilter: true } });
          }}
        >
          {filterButtonText}
        </Button>
      </div>

      <ScrollArea className="flex-1" ref={scrollAreaRef}>
        <div className="p-6">
          {/* Desktop/Tablet View */}
          <div className="hidden lg:block">
            <div className="grid grid-cols-12 gap-4 mb-4 px-4 py-2 bg-muted/50 rounded-lg font-medium text-sm">
              <div className="col-span-1">{colIdLabel}</div>
              <div className="col-span-2">{colPreviewLabel}</div>
              <div className="col-span-2">{colNameLabel}</div>
              <div className="col-span-1">{colDateLabel}</div>
              <div className="col-span-2">{colStorageLabel}</div>
              <div className="col-span-1">{colFlagsLabel}</div>
              <div className="col-span-3">{colDetailsLabel}</div>
            </div>

            <div className="space-y-3">
              {photos.map((photo) => (
                <PhotoListItemDesktop
                  key={photo.id}
                  photo={photo}
                  personsMap={personsMap}
                  tagsMap={tagsMap}
                  onClick={() => setDetailsId(photo.id)}
                />
              ))}
            </div>
          </div>

          {/* Mobile View */}
          <div className="lg:hidden">
            <div className="grid gap-4 sm:grid-cols-2">
              {photos.map((photo) => (
                <PhotoListItemMobile
                  key={photo.id}
                  photo={photo}
                  personsMap={personsMap}
                  tagsMap={tagsMap}
                  onClick={() => setDetailsId(photo.id)}
                />
              ))}
            </div>
          </div>
          {photos.length < total && (
            <div className="flex justify-center mt-4">
              <Button variant="outline" onClick={loadMore}>
                {loadMoreButton}
              </Button>
            </div>
          )}
        </div>
      </ScrollArea>
      <PhotoDetailsModal
        photoId={detailsId}
        onOpenChange={(open) => {
          if (!open) setDetailsId(null);
        }}
      />
    </div>
  );
};

export default PhotoListPage;

