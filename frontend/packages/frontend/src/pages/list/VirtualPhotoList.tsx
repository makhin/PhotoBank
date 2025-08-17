import React from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import PhotoListItemDesktop from './PhotoListItemDesktop';
import { usePhotoVirtual } from './usePhotoVirtual';

interface VirtualPhotoListProps {
  photos: PhotoItemDto[];
  parentRef: React.RefObject<HTMLElement>;
  renderRow?: (photo: PhotoItemDto) => React.ReactNode;
  estimateSize?: (index: number) => number;
}

const defaultRenderRow = (photo: PhotoItemDto) => (
  <PhotoListItemDesktop
    photo={photo}
    personsMap={{}}
    tagsMap={{}}
    onClick={() => {}}
  />
);

const VirtualPhotoList = ({
  photos,
  parentRef,
  renderRow,
  estimateSize,
}: VirtualPhotoListProps) => {
  const { items, totalSize, virtualizer } = usePhotoVirtual({
    count: photos.length,
    parentRef,
    estimateSize,
  });

  const row = renderRow ?? defaultRenderRow;

  return (
    <div
      style={{ height: `${totalSize}px`, width: '100%', position: 'relative' }}
    >
      {items.map((item) => {
        const photo = photos[item.index];
        return (
          <div
            key={photo.id}
            ref={virtualizer.measureElement}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              height: `${item.size}px`,
              transform: `translateY(${item.start}px)`,
            }}
          >
            {row(photo)}
          </div>
        );
      })}
    </div>
  );
};

export default VirtualPhotoList;
