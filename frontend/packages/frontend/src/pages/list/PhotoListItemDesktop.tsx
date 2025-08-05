import { Calendar, User, Tag } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import PhotoPreview from './PhotoPreview';
import PhotoFlags from '@/components/PhotoFlags';
import MetadataBadgeList from '@/components/MetadataBadgeList';
import { formatDate, firstNWords } from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/generated';
import {
  MAX_VISIBLE_PERSONS_LG,
  MAX_VISIBLE_TAGS_LG,
} from '@photobank/shared/constants';

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
}: PhotoListItemDesktopProps) => (
  <Card
    key={photo.id}
    className="p-4 hover:shadow-md transition-shadow cursor-pointer"
    onClick={onClick}
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
            {firstNWords(photo.captions[0], 5)}
          </div>
        )}
      </div>

      <div className="col-span-1">
        <div className="flex items-center gap-1 text-sm">
          <Calendar className="w-3 h-3" />
          {formatDate(photo.takenDate ?? undefined)}
        </div>
      </div>

      <div className="col-span-2">
        <div className="text-xs text-muted-foreground truncate">
          {photo.storageName} {photo.relativePath}
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
            items={photo.persons?.map((p) => p.personId) ?? []}
            map={personsMap}
            maxVisible={MAX_VISIBLE_PERSONS_LG}
            variant="outline"
          />
          <MetadataBadgeList
            icon={Tag}
            items={photo.tags?.map((t) => t.tagId) ?? []}
            map={tagsMap}
            maxVisible={MAX_VISIBLE_TAGS_LG}
            variant="secondary"
          />
        </div>
      </div>
    </div>
  </Card>
);

export default PhotoListItemDesktop;

