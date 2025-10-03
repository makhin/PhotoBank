import * as React from 'react';
import { format } from 'date-fns';
import type { ColumnDef } from '@tanstack/react-table';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { toDate } from '@photobank/shared/utils/parseDate';

import MetadataBadgeList from '@/components/MetadataBadgeList';
import { Badge } from '@/shared/ui/badge';
import { buildThumbnailUrl } from '@/shared/utils/buildThumbnailUrl';
import { useAppSelector } from '@/app/hook';
import {
  selectMetadataLoaded,
  selectPersonsMap,
  selectTagsMap,
} from '@/features/metadata/selectors';

export function usePhotoColumns(): ColumnDef<PhotoItemDto>[] {
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);
  const metaLoaded = useAppSelector(selectMetadataLoaded);

  return React.useMemo<ColumnDef<PhotoItemDto>[]>(
    () => [
      {
        accessorKey: 'thumbnail',
        id: 'thumbnail',
        header: 'Thumb',
        cell: ({ row }) => (
          <div className="flex items-center justify-center">
            <img
              src={buildThumbnailUrl(row.original)}
              alt={row.original.name}
              loading="lazy"
              className="w-[50px] h-[50px] object-cover rounded-md border border-thumbnail-border shadow-sm"
            />
          </div>
        ),
        size: 80,
        enableSorting: false,
        enableResizing: false,
      },
      {
        accessorKey: 'path',
        id: 'path',
        header: 'Path',
        cell: ({ row }) => (
          <div className="font-mono text-sm text-muted-foreground truncate">
            {row.original.storageName}
            {row.original.relativePath && '/' + row.original.relativePath}
            {'/' + row.original.name}
          </div>
        ),
        enableSorting: false,
        enableResizing: true,
      },
      {
        accessorKey: 'caption',
        id: 'caption',
        header: 'Caption',
        cell: ({ row }) => (
          <div className="text-sm text-foreground whitespace-pre-wrap break-words">
            {row.original.captions ? (
              row.original.captions[0]
            ) : (
              <span className="text-muted-foreground italic">No caption</span>
            )}
          </div>
        ),
        // 30% of available width
        enableSorting: false,
        enableResizing: true,
      },
      {
        accessorKey: 'takenDate',
        id: 'date',
        header: 'Date',
        accessorFn: (p) => {
          const date = toDate(p.takenDate);
          if (!date) {
            return null;
          }

          const timestamp = date.getTime();
          return Number.isNaN(timestamp) ? null : timestamp;
        },
        cell: ({ getValue }) => {
          const timestamp = getValue() as number | null;
          return (
            <div className="text-sm text-muted-foreground font-mono whitespace-nowrap">
              {timestamp ? format(new Date(timestamp), 'dd.MM.yyyy hh:mm') : null}
            </div>
          );
        },
        size: 200,
        enableSorting: false,
        enableResizing: true,
      },
      {
        accessorKey: 'tags',
        id: 'tags',
        header: 'Tags',
        cell: ({ row }) => (
          <MetadataBadgeList
            items={
              metaLoaded ? (row.original.tags ?? []) : []
            }
            map={tagsMap}
            maxVisible={8}
            variant="outline"
          />
        ),
        enableSorting: false,
        enableResizing: true,
      },
      {
        accessorKey: 'peoples',
        id: 'peoples',
        header: 'People',
        cell: ({ row }) => (
          <MetadataBadgeList
            items={
              metaLoaded ? (row.original.persons ?? []) : []
            }
            map={personsMap}
            maxVisible={6}
            variant="secondary"
          />
        ),
        enableSorting: false,
        enableResizing: true,
      },
      {
        accessorKey: 'flags',
        id: 'flags',
        header: 'Flags',
        cell: ({ row }) => (
          <>
            {row.original.isBW && <Badge variant="secondary">B/W</Badge>}
            {row.original.isAdultContent && (
              <Badge variant="destructive">NSFW</Badge>
            )}
            {row.original.isRacyContent && (
              <Badge variant="destructive">Racy</Badge>
            )}
          </>
        ),
        size: 100,
        enableSorting: false,
        enableResizing: true,
      },
    ],
    [metaLoaded, personsMap, tagsMap]
  );
}
