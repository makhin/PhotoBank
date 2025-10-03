import { Calendar, User, Tag } from 'lucide-react';
import { formatDate, firstNWords } from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import {
  MAX_VISIBLE_PERSONS_SM,
  MAX_VISIBLE_TAGS_SM,
} from '@photobank/shared/constants';

import { Card } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import PhotoFlags from '@/components/PhotoFlags';
import MetadataBadgeList from '@/components/MetadataBadgeList';

import PhotoPreview from './PhotoPreview';

export type PhotoListItemMobileProps = {
  photo: PhotoItemDto;
  personsMap: Record<number, string>;
  tagsMap: Record<number, string>;
  onClick: () => void;
};

const PhotoListItemMobile = ({
  photo,
  personsMap,
  tagsMap,
  onClick,
}: PhotoListItemMobileProps) => (
  <Card
    key={photo.id}
    className="p-4 hover:shadow-md transition-shadow cursor-pointer"
    onClick={onClick}
  >
    <div className="space-y-3">
      <div className="flex items-start gap-3">
        <PhotoPreview
          thumbnailUrl={photo.thumbnailUrl ?? ''}
          alt={photo.name}
          className="w-20 h-20 flex-shrink-0"
        />
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{photo.name}</div>
            {photo.captions && photo.captions.length > 0 && (
              <div className="text-xs text-muted-foreground truncate">
                {firstNWords(photo.captions[0] ?? '', 5)}
              </div>
            )}
          <Badge variant="outline" className="font-mono text-xs mt-1">
            {photo.id}
          </Badge>
        </div>
      </div>

      <div className="text-xs text-muted-foreground">
        {photo.storageName} {photo.relativePath}
      </div>

      <div className="flex items-center gap-4 text-sm">
        <div className="flex items-center gap-1">
          <Calendar className="w-3 h-3" />
          {formatDate(photo.takenDate ?? undefined)}
        </div>
      </div>

      <PhotoFlags
        isBW={photo.isBW}
        isAdultContent={photo.isAdultContent}
        isRacyContent={photo.isRacyContent}
      />

      <MetadataBadgeList
        icon={User}
        items={photo.persons?.map((p) => p) ?? []}
        map={personsMap}
        maxVisible={MAX_VISIBLE_PERSONS_SM}
        variant="outline"
      />

      <MetadataBadgeList
        icon={Tag}
        items={photo.tags?.map((t) => t) ?? []}
        map={tagsMap}
        maxVisible={MAX_VISIBLE_TAGS_SM}
        variant="secondary"
      />
    </div>
  </Card>
);

export default PhotoListItemMobile;

