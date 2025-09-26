import * as React from 'react';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type VisibilityState,
} from '@tanstack/react-table';
import { useVirtualizer } from '@tanstack/react-virtual';
import { type PhotoItemDto } from '@photobank/shared/api/photobank';

import { Card } from '@/shared/ui/card';

import { usePhotoColumns } from './photoColumns';

type Props = {
  rows: PhotoItemDto[];
  isFetchingNextPage?: boolean;
  hasNextPage?: boolean;
  fetchNextPage?: () => void;
  storageKey?: string; // ключ для localStorage (состояние видимости колонок)
};

const DEFAULT_HEIGHT = 640;

export function PhotoTable({
  rows,
  isFetchingNextPage,
  hasNextPage,
  fetchNextPage,
  storageKey = 'photoTable.columns',
}: Props) {
  const columns = usePhotoColumns();

  // column visibility persistence
  const [visibility, setVisibility] = React.useState<VisibilityState>(() => {
    try {
      const raw = localStorage.getItem(storageKey);
      return raw ? (JSON.parse(raw) as VisibilityState) : {};
    } catch {
      return {};
    }
  });

  React.useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(visibility));
  }, [visibility, storageKey]);

  const table = useReactTable({
    data: rows,
    columns: columns,
    state: { columnVisibility: visibility },
    onColumnVisibilityChange: setVisibility,
    getCoreRowModel: getCoreRowModel(),
  });

  // virtualization
  const parentRef = React.useRef<HTMLDivElement | null>(null);
  const rowVirtualizer = useVirtualizer({
    count: table.getRowModel().rows.length + (hasNextPage ? 1 : 0),
    getScrollElement: () => parentRef.current,
    estimateSize: () => 92, // средняя высота строки (preview 80px + отступы)
    overscan: 8,
  });

  // infinite load sentinel
  const virtualRows = rowVirtualizer.getVirtualItems();
  const last = virtualRows[virtualRows.length - 1];
  React.useEffect(() => {
    if (!last) return;
    const isLoaderRow = hasNextPage && last.index === table.getRowModel().rows.length;
    if (isLoaderRow && !isFetchingNextPage && fetchNextPage) {
      fetchNextPage();
    }
  }, [last, hasNextPage, isFetchingNextPage, fetchNextPage, table]);

  const visibleColumns = table.getVisibleLeafColumns();

  let gridTemplateColumns = '1fr';
  let minTableWidth = 0;

  if (visibleColumns.length) {
    const tracks: string[] = [];

    visibleColumns.forEach((column) => {
      const size = column.columnDef.size;
      if (typeof size === 'number' && Number.isFinite(size)) {
        tracks.push(`${size}px`);
        minTableWidth += size;
        return;
      }

      const minSize = column.columnDef.minSize;
      if (typeof minSize === 'number' && Number.isFinite(minSize)) {
        tracks.push(`minmax(${minSize}px, 1fr)`);
        minTableWidth += minSize;
        return;
      }

      const fallback = 180;
      tracks.push(`minmax(${fallback}px, 1fr)`);
      minTableWidth += fallback;
    });

    gridTemplateColumns = tracks.join(' ');
  }

  return (
    <Card className="w-full overflow-hidden">
      <div className="overflow-x-auto">
        <div className="min-w-full">
          {/* Header */}
          <div
            className="grid gap-4 px-4 py-2 border-b bg-muted/30"
            style={{
              gridTemplateColumns,
              minWidth: minTableWidth ? `${minTableWidth}px` : undefined,
            }}
          >
            {table.getFlatHeaders().map((header) => (
              <div key={header.id} className="text-xs font-medium uppercase text-muted-foreground truncate">
                {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
              </div>
            ))}
          </div>

          {/* Body with virtualization */}
          <div
            ref={parentRef}
            className="relative overflow-y-auto"
            style={{
              minWidth: minTableWidth ? `${minTableWidth}px` : undefined,
              maxHeight: `var(--photo-table-max-height, ${DEFAULT_HEIGHT}px)`,
            }}
          >
            <div
              style={{
                height: rowVirtualizer.getTotalSize(),
                position: 'relative',
              }}
            >
              {virtualRows.map((vr) => {
                const isLoaderRow = hasNextPage && vr.index === table.getRowModel().rows.length;
                const row = table.getRowModel().rows[vr.index];

                return (
                  <div
                    key={vr.key}
                    data-index={vr.index}
                    ref={rowVirtualizer.measureElement}
                    className="grid gap-4 px-4 py-3 items-start"
                    style={{
                      gridTemplateColumns,
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      width: '100%',
                      transform: `translateY(${vr.start}px)`,
                    }}
                  >
                    {isLoaderRow ? (
                      <div
                        className="text-center text-sm text-muted-foreground py-3"
                        style={{ gridColumn: '1 / -1' }}
                      >
                        {isFetchingNextPage ? 'Loading more…' : 'Load more'}
                      </div>
                    ) : row ? (
                      row.getVisibleCells().map((cell) => (
                        <div key={cell.id} className="min-w-0">
                          {flexRender(cell.column.columnDef.cell, cell.getContext())}
                        </div>
                      ))
                    ) : null}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}