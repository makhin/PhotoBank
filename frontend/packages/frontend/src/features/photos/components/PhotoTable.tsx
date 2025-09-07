import * as React from 'react';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  ColumnDef,
  VisibilityState,
} from '@tanstack/react-table';
import { useVirtualizer } from '@tanstack/react-virtual';
import { Card } from '@/components/ui/card';
import { usePhotoColumns } from './photoColumns';
import type { PhotoItemDto } from '@/shared/types';

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
    columns: columns as ColumnDef<PhotoItemDto, any>[],
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

  return (
    <Card className="w-full overflow-hidden">
      {/* Header */}
      <div className="grid grid-cols-[96px_1fr_120px_240px_320px_140px] gap-4 px-4 py-2 border-b bg-muted/30">
        {table.getFlatHeaders().map((header) => (
          <div key={header.id} className="text-xs font-medium uppercase text-muted-foreground truncate">
            {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
          </div>
        ))}
      </div>

      {/* Body with virtualization */}
      <div
        ref={parentRef}
        style={{ height: DEFAULT_HEIGHT, overflow: 'auto', position: 'relative' }}
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
                className="grid grid-cols-[96px_1fr_120px_240px_320px_140px] gap-4 px-4 py-3 items-start"
                style={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  width: '100%',
                  transform: `translateY(${vr.start}px)`,
                }}
              >
                {isLoaderRow ? (
                  <div className="col-span-6 text-center text-sm text-muted-foreground py-3">
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
    </Card>
  );
}