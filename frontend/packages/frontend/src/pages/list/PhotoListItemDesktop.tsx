import { Calendar, User, Tag } from 'lucide-react';
import { formatDate, firstNWords } from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  MAX_VISIBLE_PERSONS_LG,
  MAX_VISIBLE_TAGS_LG,
} from '@photobank/shared/constants';

import { Card } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import PhotoFlags from '@/components/PhotoFlags';
import MetadataBadgeList from '@/components/MetadataBadgeList';

import PhotoPreview from './PhotoPreview';
import { memo, useMemo, useEvent } from 'react';

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
  const handleClick = useEvent(onClick);
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

  return (
    <Card
      className="p-4 mb-3 hover:shadow-md transition-shadow cursor-pointer"
      onClick={handleClick}
    >
      <div className="grid grid-cols-12 gap-4 items-center">
        <div className="col-span-1">
          <Badge variant="outline" className="font-mono text-xs">
            {photo.id}
          </Badge>
        </div>

        <div className="col-span-2">
          <PhotoPreview
            thumbnail={photo.thumbnail}
            alt={photo.name}
            className="w-16 h-16"
          />
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

