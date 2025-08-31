import { Calendar, User, Tag } from 'lucide-react';
import { firstNWords } from '@photobank/shared';
import { formatDate } from '@photobank/shared/format';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  MAX_VISIBLE_PERSONS_LG,
  MAX_VISIBLE_TAGS_LG,
} from '@photobank/shared/constants';
import { memo, useMemo, useCallback } from 'react';

import { Card } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import PhotoFlags from '@/components/PhotoFlags';
import MetadataBadgeList from '@/components/MetadataBadgeList';
import SmartImage from '@/components/SmartImage';
import { useInView } from '@/hooks/useInView';
import { usePrefetchOnHover } from '@/hooks/useImagePrefetch';

export type PhotoListItemDesktopProps = {
  photo: PhotoItemDto;
  personsMap: Record<number, string>;
  tagsMap: Record<number, string>;
  onClick: () => void;
};

const PhotoListItemDesktop = ({
  photo,
  personsMap,
  tagsMap,
  onClick,
}: PhotoListItemDesktopProps) => {
  const handleClick = useCallback(() => {
    onClick();
  }, [onClick]);
  const caption = useMemo(
    () => firstNWords(photo.captions?.[0] ?? '', 5),
    [photo.captions]
  );
  const takenDate = useMemo(
    () => formatDate(photo.takenDate ?? undefined),
    [photo.takenDate]
  );
  const personIds = useMemo(
    () => photo.persons?.map((p) => p.personId) ?? [],
    [photo.persons]
  );
  const tagIds = useMemo(
    () => photo.tags?.map((t) => t.tagId) ?? [],
    [photo.tags]
  );
  const storagePath = useMemo(
    () => `${photo.storageName} ${photo.relativePath}`,
    [photo.storageName, photo.relativePath]
  );

  const [ref, inView] = useInView<HTMLDivElement>();
  const prefetchHandlers = usePrefetchOnHover([
    photo.thumbnailUrl ?? '',
  ]);

  const base = photo.thumbnailUrl;
  const srcSet = base
    ? [
        `${base}?w=480 480w`,
        `${base}?w=960 960w`,
        `${base}?w=1440 1440w`,
      ].join(', ')
    : undefined;
  const sizes = base
    ? '(max-width: 640px) 480px, (max-width: 1024px) 960px, 1440px'
    : undefined;

  return (
    <Card
      className="p-4 mb-3 hover:shadow-md transition-shadow cursor-pointer"
      onClick={handleClick}
      {...prefetchHandlers}
    >
      <div className="grid grid-cols-12 gap-4 items-center">
        <div className="col-span-1">
          <Badge variant="outline" className="font-mono text-xs">
            {photo.id}
          </Badge>
        </div>

        <div className="col-span-2">
          <div ref={ref} className="w-16 h-16">
            {inView && (
              <SmartImage
                alt={photo.name}
                thumbSrc={photo.thumbnailUrl ?? ''}
                src={photo.thumbnailUrl ?? ''}
                srcSet={srcSet}
                sizes={sizes}
                className="w-full h-full rounded-lg"
              />
            )}
          </div>
        </div>

        <div className="col-span-2">
          <div className="font-medium truncate">{photo.name}</div>
          {photo.captions && photo.captions.length > 0 && (
            <div className="text-xs text-muted-foreground truncate">
              {caption}
            </div>
          )}
        </div>

        <div className="col-span-1">
          <div className="flex items-center gap-1 text-sm">
            <Calendar className="w-3 h-3" />
            {takenDate}
          </div>
        </div>

        <div className="col-span-2">
          <div className="text-xs text-muted-foreground truncate">
            {storagePath}
          </div>
        </div>

        <div className="col-span-1">
          <PhotoFlags
            isBW={photo.isBW}
            isAdultContent={photo.isAdultContent}
            isRacyContent={photo.isRacyContent}
          />
        </div>

        <div className="col-span-3">
          <div className="space-y-2">
            <MetadataBadgeList
              icon={User}
              items={personIds}
              map={personsMap}
              maxVisible={MAX_VISIBLE_PERSONS_LG}
              variant="outline"
            />
            <MetadataBadgeList
              icon={Tag}
              items={tagIds}
              map={tagsMap}
              maxVisible={MAX_VISIBLE_TAGS_LG}
              variant="secondary"
            />
          </div>
        </div>
      </div>
    </Card>
  );
};

export default memo(PhotoListItemDesktop);

