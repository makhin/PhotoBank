import React, { useRef } from 'react';
import type { CSSProperties } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
} from '@tanstack/react-table';
import { useWindowVirtualizer } from '@tanstack/react-virtual';
import { type PhotoItemDto } from '@photobank/shared/api/photobank';

import { usePhotoColumns } from './photoColumns';

type Props = {
  photos: PhotoItemDto[];
  fetchNextPage?: () => void;
  hasNextPage?: boolean;
  isFetchingNextPage?: boolean;
  onRowClick?: (photoId: number) => void;
};

const CONTENT_BASE_FLEX_STYLE: CSSProperties = {
  flexGrow: 1,
  flexShrink: 1,
  flexBasis: 0,
  minWidth: 0,
};

const DEFAULT_COLUMN_FLEX_STYLE: CSSProperties = CONTENT_BASE_FLEX_STYLE;
const COLUMN_FLEX_STYLES: Record<string, CSSProperties> = {
  thumbnail: { flexBasis: '80px', flexShrink: 0 },
  flags: { flexBasis: '100px', flexShrink: 0 },
  date: { flexBasis: '150px', flexShrink: 0 },
  path: CONTENT_BASE_FLEX_STYLE,
  caption: { ...CONTENT_BASE_FLEX_STYLE, flexGrow: 1.5 },
};

const HEADER_HEIGHT = 52;

function getColumnFlexStyle(columnId: string): CSSProperties {
  return COLUMN_FLEX_STYLES[columnId] ?? DEFAULT_COLUMN_FLEX_STYLE;
}

export function PhotoTable({
                             photos,
                             fetchNextPage,
                             hasNextPage,
                             isFetchingNextPage,
                             onRowClick,
                           }: Props) {
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const [scrollMargin, setScrollMargin] = React.useState(0);

  const table = useReactTable({
    data: photos,
    columns: usePhotoColumns(),
    getCoreRowModel: getCoreRowModel(),
  });

  const { rows } = table.getRowModel();

  React.useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const updateScrollMargin = () => {
      if (!tableContainerRef.current) {
        setScrollMargin(0);
        return;
      }
      const rect = tableContainerRef.current.getBoundingClientRect();
      const nextMargin = Math.max(0, rect.top + window.scrollY);
      setScrollMargin((prev) =>
        Math.abs(prev - nextMargin) < 1 ? prev : nextMargin
      );
    };

    updateScrollMargin();

    window.addEventListener('resize', updateScrollMargin);

    return () => {
      window.removeEventListener('resize', updateScrollMargin);
    };
  }, [rows.length]);

  const rowVirtualizer = useWindowVirtualizer({
    count: rows.length,
    estimateSize: () => 80,
    overscan: 10,
    scrollMargin,
  });

  React.useEffect(() => {
    if (
      typeof window === 'undefined' ||
      !hasNextPage ||
      isFetchingNextPage ||
      !fetchNextPage
    ) {
      return;
    }

    const lastItem = rowVirtualizer.getVirtualItems().at(-1);
    if (!lastItem) {
      return;
    }

    const tableBottom =
      scrollMargin + HEADER_HEIGHT + rowVirtualizer.getTotalSize();
    const viewportBottom = window.scrollY + window.innerHeight;
    const distanceToBottom = Math.max(0, tableBottom - viewportBottom);
    const triggerThreshold = lastItem.size ?? 150;

    if (distanceToBottom <= triggerThreshold) {
      fetchNextPage();
    }
  }, [
    rows.length,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    rowVirtualizer,
    scrollMargin,
  ]);

  return (
    <div ref={tableContainerRef} className="w-full bg-background">
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
      <div
        className="relative"
        style={{ height: `${rowVirtualizer.getTotalSize()}px` }}
      >
        {rowVirtualizer.getVirtualItems().map((virtualRow) => {
          const row = rows[virtualRow.index];
          if (!row) return null;
          const rowKey = row.original.id ?? row.id;
          const translateY = Math.max(0, virtualRow.start - scrollMargin);
          return (
            <div
              key={rowKey}
              role="button"
              tabIndex={0}
              onClick={() => onRowClick?.(row.original.id)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  onRowClick?.(row.original.id);
                }
              }}
              className="absolute top-0 left-0 w-full flex items-center border-b border-border hover:bg-gallery-hover transition-colors duration-150 cursor-pointer outline-none focus-visible:ring-2 focus-visible:ring-primary"
              style={{
                height: `${virtualRow.size}px`,
                transform: `translateY(${translateY}px)`,
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
