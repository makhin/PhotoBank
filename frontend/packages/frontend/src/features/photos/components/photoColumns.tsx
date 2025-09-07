import * as React from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Badge } from '@/shared/ui/badge';
import { buildThumbnailUrl } from '@/shared/utils/buildThumbnailUrl';
import { useAppSelector } from '@/app/hook';
import { selectPersonsMap, selectTagsMap, selectMetadataLoaded } from '@/features/metadata/selectors';
import type { PhotoItemDto } from '@/shared/types';

export function usePhotoColumns(): ColumnDef<PhotoItemDto, any>[] {
  const personsMap = useAppSelector(selectPersonsMap);
  const tagsMap = useAppSelector(selectTagsMap);
  const metaLoaded = useAppSelector(selectMetadataLoaded);

  return React.useMemo<ColumnDef<PhotoItemDto, any>[]>(() => [
    {
      id: 'preview',
      header: 'Preview',
      size: 96,
      cell: ({ row }) => {
        const p = row.original;
        const src = buildThumbnailUrl(p);
        const alt = p.fileName ?? `photo-${p.id}`;
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
            <div className="font-medium leading-tight">{p.caption || p.fileName}</div>
            {p.relativePath && (
              <div className="text-xs text-muted-foreground truncate">{p.relativePath}</div>
            )}
          </div>
        );
      },
    },
    {
      id: 'date',
      header: 'Taken',
      accessorFn: (p) => p.takenDate ?? p.createdAt ?? '',
      cell: ({ getValue }) => {
        const v = getValue() as string | Date | undefined;
        if (!v) return null;
        const d = new Date(v);
        return <span className="text-sm">{d.toLocaleDateString()}</span>;
      },
      size: 120,
    },
    {
      id: 'people',
      header: 'People',
      cell: ({ row }) => {
        if (!metaLoaded) return null;
        const faces = row.original.faces ?? [];
        const names = Array.from(
          new Set(
            faces
              .map((f) => (f.personId ? personsMap.get(f.personId) : null))
              .filter(Boolean) as string[],
          ),
        );
        if (names.length === 0) return null;
        return (
          <div className="flex flex-wrap gap-1">
            {names.slice(0, 6).map((n) => (
              <Badge key={n} variant="secondary">{n}</Badge>
            ))}
            {names.length > 6 && (
              <span className="text-xs text-muted-foreground">+{names.length - 6}</span>
            )}
          </div>
        );
      },
      size: 240,
    },
    {
      id: 'tags',
      header: 'Tags',
      cell: ({ row }) => {
        if (!metaLoaded) return null;
        const p = row.original as any;
        // поддерживаем и старый вид (photo.photoTags[].tagId), и новый (tags: number[] | string[])
        const tagIds: number[] = Array.isArray(p.photoTags)
          ? p.photoTags.map((t: any) => t?.tagId).filter(Boolean)
          : Array.isArray(p.tags)
            ? p.tags.map((t: any) => (typeof t === 'number' ? t : t?.tagId)).filter(Boolean)
            : [];

        const names = tagIds
          .map((id: number) => tagsMap.get(id))
          .filter(Boolean) as string[];

        if (names.length === 0) return null;
        return (
          <div className="flex flex-wrap gap-1">
            {names.slice(0, 8).map((n) => (
              <Badge key={n} variant="outline">{n}</Badge>
            ))}
            {names.length > 8 && (
              <span className="text-xs text-muted-foreground">+{names.length - 8}</span>
            )}
          </div>
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