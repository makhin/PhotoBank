import type { FC } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import type { PersonMap, TagMap } from '@photobank/shared/metadata';

import EmptyState from '@/components/EmptyState';

import PhotoListItemMobile from './PhotoListItemMobile';

interface PhotoListMobileProps {
  loading: boolean;
  photos: PhotoItemDto[];
  skeletonPhotos: PhotoItemDto[];
  personsMap: PersonMap;
  tagsMap: TagMap;
  onPhotoClick: (id: number) => void;
}

const PhotoListMobile: FC<PhotoListMobileProps> = ({
  loading,
  photos,
  skeletonPhotos,
  personsMap,
  tagsMap,
  onPhotoClick,
}) => {
  return (
    <div className="block lg:hidden">
      <div className="grid gap-4 sm:grid-cols-2">
        {loading
          ? skeletonPhotos.slice(0, 6).map((photo: PhotoItemDto) => (
              <div
                key={photo.id}
                className="h-40 w-full rounded-lg bg-muted animate-pulse"
              />
            ))
          : photos.length === 0
            ? (
                <EmptyState text="No photos" />
              )
            : (
                photos.map((photo: PhotoItemDto) => (
                  <PhotoListItemMobile
                    key={photo.id}
                    photo={photo}
                    personsMap={personsMap}
                    tagsMap={tagsMap}
                    onClick={() => onPhotoClick(photo.id)}
                  />
                ))
              )}
      </div>
    </div>
  );
};

export default PhotoListMobile;
