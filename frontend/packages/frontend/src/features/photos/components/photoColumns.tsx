import * as React from 'react';
import { format, formatISO, isValid, parseISO } from 'date-fns';
import type { ColumnDef } from '@tanstack/react-table';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import MetadataBadgeList from '@/components/MetadataBadgeList';
import { Badge } from '@/shared/ui/badge';
import { buildThumbnailUrl } from '@/shared/utils/buildThumbnailUrl';
import { useAppSelector } from '@/app/hook';
import {
  selectPersonsMap,
  selectTagsMap,
  selectMetadataLoaded,
} from '@/features/metadata/selectors';

export function usePhotoColumns(): ColumnDef<PhotoItemDto>[] {
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);
  const metaLoaded = useAppSelector(selectMetadataLoaded);

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
        const items = metaLoaded
          ? row.original.persons?.map((person) => person.personId) ?? []
          : [];
        return (
          <MetadataBadgeList
            items={items}
            map={personsMap}
            maxVisible={6}
            variant="secondary"
          />
        );
      },
      size: 240,
    },
    {
      id: 'tags',
      header: 'Tags',
      cell: ({ row }) => {
        const items = metaLoaded
          ? row.original.tags?.map((tag) => tag.tagId) ?? []
          : [];
        return (
          <MetadataBadgeList
            items={items}
            map={tagsMap}
            maxVisible={8}
            variant="outline"
          />
        );
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
  ], [metaLoaded, personsMap, tagsMap]);
}
