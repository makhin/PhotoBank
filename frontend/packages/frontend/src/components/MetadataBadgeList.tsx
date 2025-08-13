import type { LucideIcon } from 'lucide-react';
import type { ComponentProps } from 'react';

import { Badge } from '@/components/ui/badge';

export type MetadataBadgeListProps = {
  icon: LucideIcon;
  items: number[];
  map: Record<number, string>;
  maxVisible: number;
  variant?: ComponentProps<typeof Badge>['variant'];
};

const MetadataBadgeList = ({
  icon: Icon,
  items,
  map,
  maxVisible,
  variant = 'outline',
}: MetadataBadgeListProps) => {
  if (!items || items.length === 0) return null;
  return (
    <div className="flex items-center gap-1 flex-wrap">
      <Icon className="w-3 h-3 text-muted-foreground" />
      {items.slice(0, maxVisible).map((id, index) => (
        <Badge key={index} variant={variant} className="text-xs">
          {map[id] ?? id}
        </Badge>
      ))}
      {items.length > maxVisible && (
        <Badge variant={variant} className="text-xs">
          +{items.length - maxVisible}
        </Badge>
      )}
    </div>
  );
};

export default MetadataBadgeList;

