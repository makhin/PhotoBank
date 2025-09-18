import * as React from 'react';
import { format, formatISO, isValid, parseISO } from 'date-fns';
import type { ColumnDef } from '@tanstack/react-table';
import type {
  PhotoItemDto,
  PersonItemDto,
  TagItemDto,
} from '@photobank/shared/api/photobank';

import { Badge } from '@/shared/ui/badge';
import { buildThumbnailUrl } from '@/shared/utils/buildThumbnailUrl';
import { useAppSelector } from '@/app/hook';
import {
  selectPersonsMap,
  selectTagsMap,
  selectMetadataLoaded,
} from '@/features/metadata/selectors';

function BadgeList({
                     items,
                     max = 8,
                     variant = 'outline',
                   }: {
  items: string[];
  max?: number;
  variant?: 'outline' | 'secondary' | 'destructive' | null | undefined;
}) {
  if (!items.length) return null;
  const visible = items.slice(0, max);
  const rest = items.length - visible.length;

  return (
    <div className="flex flex-wrap gap-1">
      {visible.map((n) => (
        <Badge key={n} variant={variant ?? 'outline'}>
          {n}
        </Badge>
      ))}
      {rest > 0 && <span className="text-xs text-muted-foreground">+{rest}</span>}
    </div>
  );
}

function extractNames<T>(
  items: T[] | null | undefined,
  pickId: (item: T) => number | null | undefined,
  dict: Map<number, string>,
  enabled: boolean,
): string[] {
  if (!enabled || !items?.length) return [];
  const names = items
    .map((it) => {
      const id = pickId(it);
      return id != null ? dict.get(id) : undefined;
    })
    .filter(Boolean) as string[];
  return Array.from(new Set(names));
}

export function usePhotoColumns(): ColumnDef<PhotoItemDto>[] {
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);
  const metaLoaded = useAppSelector(selectMetadataLoaded);

  const extractPersonNames = React.useCallback(
    (persons: PersonItemDto[] | null | undefined) =>
      extractNames(persons, (p) => p.personId, personsMap, metaLoaded),
    [personsMap, metaLoaded],
  );

  const extractTagNames = React.useCallback(
    (tags: TagItemDto[] | null | undefined) =>
      extractNames(tags, (t) => t.tagId, tagsMap, metaLoaded),
    [tagsMap, metaLoaded],
  );

  return React.useMemo<ColumnDef<PhotoItemDto>[]>(() => [
    {
      id: 'preview',
      header: 'Preview',
      size: 96,
      cell: ({ row }) => {
        const p = row.original;
        const src = buildThumbnailUrl(p);
        const alt = p.name ?? `photo-${p.id}`;
        return (
          <img
            src={src}
            alt={alt}
            loading="lazy"
            className="w-20 h-20 object-cover rounded-xl"
          />
        );
      },
    },
    {
      id: 'caption',
      header: 'Caption',
      cell: ({ row }) => {
        const p = row.original;
        return (
          <div className="flex flex-col">
            <div className="font-medium leading-tight">{p.name}</div>
            {p.relativePath && (
              <div className="text-xs text-muted-foreground truncate">
                {p.relativePath}
              </div>
            )}
          </div>
        );
      },
    },
    {
      id: 'date',
      header: 'Taken',
      accessorFn: (p) => {
        const rawValue = p.takenDate;
        if (!rawValue) return '';
        const isoInput =
          typeof rawValue === 'string'
            ? rawValue
            : rawValue instanceof Date
              ? rawValue.toISOString()
              : '';
        if (!isoInput) return '';
        const parsed = parseISO(isoInput);
        return isValid(parsed) ? formatISO(parsed) : '';
      },
      cell: ({ getValue }) => {
        const iso = getValue() as string;
        if (!iso) return null;
        const parsed = parseISO(iso);
        if (!isValid(parsed)) return null;
        return <span className="text-sm">{format(parsed, 'dd.MM.yyyy')}</span>;
      },
      size: 120,
    },
    {
      id: 'people',
      header: 'People',
      cell: ({ row }) => {
        const names = extractPersonNames(row.original.persons);
        return <BadgeList items={names} max={6} variant="secondary" />;
      },
      size: 240,
    },
    {
      id: 'tags',
      header: 'Tags',
      cell: ({ row }) => {
        const names = extractTagNames(row.original.tags);
        return <BadgeList items={names} max={8} variant="outline" />;
      },
      size: 320,
    },
    {
      id: 'flags',
      header: 'Flags',
      cell: ({ row }) => {
        const p = row.original;
        return (
          <div className="flex gap-1 text-xs text-muted-foreground">
            {p.isBW && <Badge variant="secondary">B/W</Badge>}
            {p.isAdultContent && <Badge variant="destructive">NSFW</Badge>}
            {p.isRacyContent && <Badge variant="destructive">Racy</Badge>}
          </div>
        );
      },
      size: 140,
    },
  ], [extractPersonNames, extractTagNames]);
}
