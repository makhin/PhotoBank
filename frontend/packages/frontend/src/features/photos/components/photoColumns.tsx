import * as React from 'react';
import { format } from 'date-fns';
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

  return React.useMemo(() => [
    {
      id: 'preview',
      header: 'Preview',
      size: 96,
      cell: ({ row }) => (
        <img
          src={buildThumbnailUrl(row.original)}
          alt={row.original.name}
          loading="lazy"
          className="w-20 h-20 object-cover rounded-xl"
        />
      ),
    },
    {
      id: 'name',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex flex-col">
          {row.original.storageName}
          {row.original.relativePath && (
            <div className="text-xs text-muted-foreground truncate">
              {row.original.relativePath}
            </div>
          )}
          <div className="font-medium leading-tight">{row.original.name}</div>
        </div>
      ),
    },
    {
      id: 'date',
      header: 'Taken',
      accessorFn: (p) => {
        const date = new Date(p.takenDate as string);
        return !Number.isNaN(date.getTime()) ? date.getTime() : null;
      },
      cell: ({ getValue }) => {
        const timestamp = getValue() as number | null;
        return timestamp ? (
          <span className="text-sm">
            {format(new Date(timestamp), 'dd.MM.yyyy hh:mm')}
          </span>
        ) : null;
      },
      size: 120,
    },
    {
      id: 'people',
      header: 'People',
      cell: ({ row }) => (
        <MetadataBadgeList
          items={metaLoaded ? row.original.persons?.map((p) => p.personId) ?? [] : []}
          map={personsMap}
          maxVisible={6}
          variant="secondary"
        />
      ),
      size: 240,
    },
    {
      id: 'tags',
      header: 'Tags',
      cell: ({ row }) => (
        <MetadataBadgeList
          items={metaLoaded ? row.original.tags?.map((t) => t.tagId) ?? [] : []}
          map={tagsMap}
          maxVisible={8}
          variant="outline"
        />
      ),
      size: 320,
    },
    {
      id: 'flags',
      header: 'Flags',
      cell: ({ row }) => (
        <div className="flex gap-1 text-xs text-muted-foreground">
          {row.original.isBW && <Badge variant="secondary">B/W</Badge>}
          {row.original.isAdultContent && <Badge variant="destructive">NSFW</Badge>}
          {row.original.isRacyContent && <Badge variant="destructive">Racy</Badge>}
        </div>
      ),
      size: 140,
    },
  ], [metaLoaded, personsMap, tagsMap]);
}