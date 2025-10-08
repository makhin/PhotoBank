import type { LucideIcon } from 'lucide-react';
import type { ComponentProps } from 'react';

import { Badge } from '@/shared/ui/badge';

type MetadataValue =
  | string
  | number
  | null
  | undefined
  | { name?: string | null | undefined };

export type MetadataLookupMap =
  | ReadonlyMap<number, MetadataValue>
  | Record<number, MetadataValue>;

export type MetadataBadgeListProps = {
  icon?: LucideIcon;
  items?: Array<number | string>;
  map?: MetadataLookupMap;
  maxVisible: number;
  variant?: ComponentProps<typeof Badge>['variant'];
};

const MetadataBadgeList = ({
  icon: Icon,
  items = [],
  map,
  maxVisible,
  variant = 'outline',
}: MetadataBadgeListProps) => {
  if (!items || items.length === 0) return null;

  const seen = new Set<string>();
  const getValueFromMap = (key: number): MetadataValue => {
    if (!map) {
      return undefined;
    }

    if (map instanceof Map) {
      return map.get(key);
    }

    return (map as Record<number, MetadataValue>)[key];
  };

  const resolveLabel = (value: MetadataValue, fallback: number | string) => {
    if (typeof value === 'string' || typeof value === 'number') {
      return value;
    }

    if (value && typeof value === 'object' && 'name' in value) {
      const name = value.name;
      if (name != null && name !== '') {
        return name;
      }
    }

    return fallback;
  };

  const resolved = items.reduce<Array<string | number>>((acc, item) => {
    const label =
      typeof item === 'string'
        ? item
        : resolveLabel(getValueFromMap(item), item);

    if (label == null || label === '') {
      return acc;
    }

    const key = typeof label === 'string' ? label : String(label);
    if (seen.has(key)) {
      return acc;
    }

    seen.add(key);
    acc.push(label);
    return acc;
  }, []);

  if (resolved.length === 0) {
    return null;
  }

  const visible = resolved.slice(0, maxVisible);
  const rest = resolved.length - visible.length;

  return (
    <div className="flex items-center gap-1 flex-wrap">
      {Icon ? <Icon className="w-3 h-3 text-muted-foreground" /> : null}
      {visible.map((label, index) => (
        <Badge key={`${String(label)}-${index}`} variant={variant} className="text-xs">
          {label}
        </Badge>
      ))}
      {rest > 0 && (
        <Badge variant={variant} className="text-xs">
          +{rest}
        </Badge>
      )}
    </div>
  );
};

export default MetadataBadgeList;

