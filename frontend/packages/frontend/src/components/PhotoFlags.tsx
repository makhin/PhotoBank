import { Badge } from '@/components/ui/badge';

export type PhotoFlagsProps = {
  isBW?: boolean;
  isAdultContent?: boolean;
  isRacyContent?: boolean;
};

const PhotoFlags = ({ isBW, isAdultContent, isRacyContent }: PhotoFlagsProps) => (
  <div className="flex flex-wrap gap-1">
    {isBW && (
      <Badge variant="secondary" className="text-xs">
        B&W
      </Badge>
    )}
    {isAdultContent && (
      <Badge variant="destructive" className="text-xs">
        Adult
      </Badge>
    )}
    {isRacyContent && (
      <Badge variant="destructive" className="text-xs">
        Racy
      </Badge>
    )}
  </div>
);

export default PhotoFlags;

