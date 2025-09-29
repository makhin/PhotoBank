import React, { useRef } from 'react';
import type { CSSProperties } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
} from '@tanstack/react-table';
import { useVirtualizer } from '@tanstack/react-virtual';
import { type PhotoItemDto } from '@photobank/shared/api/photobank';

import { usePhotoColumns } from './photoColumns';

type Props = {
  photos: PhotoItemDto[];
  fetchNextPage?: () => void;
  hasNextPage?: boolean;
  isFetchingNextPage?: boolean;
};

const DEFAULT_COLUMN_FLEX_STYLE: CSSProperties = { flex: '1.5 1 0%' };
const COLUMN_FLEX_STYLES: Record<string, CSSProperties> = {
  thumbnail: { flexBasis: '80px', flexShrink: 0 },
  flags: { flexBasis: '100px', flexShrink: 0 },
  takenDate: { flexBasis: '150px', flexShrink: 0 },
  path: { flex: '1 1 0%' },
  caption: { flex: '1.5 1 0%' },
};

function getColumnFlexStyle(columnId: string): CSSProperties {
  return COLUMN_FLEX_STYLES[columnId] ?? DEFAULT_COLUMN_FLEX_STYLE;
}

export function PhotoTable({
                             photos,
                             fetchNextPage,
                             hasNextPage,
                             isFetchingNextPage,
                           }: Props) {
  const tableContainerRef = useRef<HTMLDivElement>(null);

  const table = useReactTable({
    data: photos,
    columns: usePhotoColumns(),
    getCoreRowModel: getCoreRowModel(),
  });

  const { rows } = table.getRowModel();

  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 80,
    overscan: 10,
  });

  React.useEffect(() => {
    const lastItem = rowVirtualizer.getVirtualItems().at(-1);
    if (
      lastItem &&
      lastItem.index >= rows.length - 10 &&
      hasNextPage &&
      !isFetchingNextPage &&
      fetchNextPage
    ) {
      fetchNextPage();
    }
  }, [rows.length, hasNextPage, isFetchingNextPage, fetchNextPage, rowVirtualizer]);

  return (
    <div className="w-full h-full flex flex-col bg-background">
      {/* Table Container */}
      <div
        ref={tableContainerRef}
        className="flex-1 overflow-auto"
        style={{ contain: 'strict' }}
      >
        <div style={{ height: `${rowVirtualizer.getTotalSize()}px` }}>
          {/* Table Header */}
          <div className="sticky top-0 z-10 bg-card border-b border-border shadow-sm h-[52px]">
            {table.getHeaderGroups().map((headerGroup) => (
              <div key={headerGroup.id} className="flex w-full">
                {headerGroup.headers.map((header) => {
                  return (
                    <div
                      key={header.id}
                      className="px-4 py-3 text-left relative group"
                      style={getColumnFlexStyle(header.column.id)}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                      {header.column.getCanResize() && (
                        // eslint-disable-next-line jsx-a11y/no-static-element-interactions
                        <div
                          onMouseDown={header.getResizeHandler()}
                          onTouchStart={header.getResizeHandler()}
                          className="absolute right-0 top-0 h-full w-1 bg-border hover:bg-primary cursor-col-resize opacity-0 group-hover:opacity-100 transition-opacity"
                          style={{
                            transform: header.column.getIsResizing()
                              ? 'scaleX(1.5)'
                              : '',
                          }}
                        />
                      )}
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
          {/* Virtualized Rows */}
          <div className="relative" style={{ paddingTop: '52px' }}>
            {rowVirtualizer.getVirtualItems().map((virtualRow) => {
              const row = rows[virtualRow.index];
              if (!row) return null;
              const rowKey = row.original.id ?? row.id;
              return (
                <div
                  key={rowKey}
                  className="absolute top-0 left-0 w-full flex items-center border-b border-border hover:bg-gallery-hover transition-colors duration-150"
                  style={{
                    height: `${virtualRow.size}px`,
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                >
                  {row.getVisibleCells().map((cell) => (
                    <div
                      key={cell.id}
                      className="px-4 py-2 flex items-center"
                      style={getColumnFlexStyle(cell.column.id)}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </div>
                  ))}
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Loading indicator */}
      {isFetchingNextPage && (
        <div className="border-t border-border px-6 py-3 bg-muted">
          <div className="flex items-center justify-center text-muted-foreground">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary mr-2"></div>
            Loading more photos...
          </div>
        </div>
      )}
    </div>
  );
}
