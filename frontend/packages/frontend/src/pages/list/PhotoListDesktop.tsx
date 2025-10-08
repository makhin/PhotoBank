import type { FC } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import { PhotoTable } from '@/features/photos/components/PhotoTable';

interface PhotoListDesktopProps {
  photos: PhotoItemDto[];
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  fetchNextPage: () => void;
  onRowClick: (id: number) => void;
}

const PhotoListDesktop: FC<PhotoListDesktopProps> = ({
  photos,
  hasNextPage,
  isFetchingNextPage,
  fetchNextPage,
  onRowClick,
}) => {
  return (
    <div className="hidden h-screen w-full overflow-hidden lg:block">
      <PhotoTable
        photos={photos}
        hasNextPage={hasNextPage}
        isFetchingNextPage={isFetchingNextPage}
        fetchNextPage={fetchNextPage}
        onRowClick={onRowClick}
      />
    </div>
  );
};

export default PhotoListDesktop;
