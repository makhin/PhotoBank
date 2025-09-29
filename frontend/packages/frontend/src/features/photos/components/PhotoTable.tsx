import React, { useRef } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  type ColumnSizingState,
} from '@tanstack/react-table';
import { useVirtualizer } from '@tanstack/react-virtual';
import { type PhotoItemDto } from '@photobank/shared/api/photobank';

import { usePhotoColumns } from './photoColumns';

type Props = {
  photos: PhotoItemDto[];
  fetchNextPage?: () => void;
  hasNextPage?: boolean;
  isFetchingNextPage?: boolean;
  storageKey?: string; // ключ для localStorage (состояние видимости колонок)
};

export function PhotoTable({
                             photos,
                             fetchNextPage,
                             hasNextPage,
                             isFetchingNextPage,
                           }: Props) {
  const [columnSizing, setColumnSizing] = React.useState<ColumnSizingState>({});
  const tableContainerRef = useRef<HTMLDivElement>(null);

  const table = useReactTable({
    data: photos,
    columns: usePhotoColumns(),
    state: { columnSizing },
    onColumnSizingChange: setColumnSizing,
    getCoreRowModel: getCoreRowModel(),
    enableColumnResizing: true,
    columnResizeMode: 'onChange',
  });

  const { rows } = table.getRowModel();

  const rowVirtualizer = useVirtualizer({
    count: table.getRowModel().rows.length + (hasNextPage ? 1 : 0),
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
              <div key={headerGroup.id} className="flex">
                {headerGroup.headers.map((header) => (
                  <div
                    key={header.id}
                    className="px-4 py-3 text-left relative group shrink-0"
                    style={{ width: header.getSize() }}
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
                ))}
              </div>
            ))}
          </div>

          {/* Virtualized Rows */}
          <div className="relative" style={{ paddingTop: '52px' }}>
            {rowVirtualizer.getVirtualItems().map((virtualRow) => {
              const photoItemDto = photos[virtualRow.index];
              if (!photoItemDto) return null;
              return (
                <div
                  key={photoItemDto.id}
                  className="absolute top-0 left-0 w-full flex items-center border-b border-border hover:bg-gallery-hover transition-colors duration-150"
                  style={{
                    height: `${virtualRow.size}px`,
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                >
                  {table.getRowModel().rows[virtualRow.index]?.getVisibleCells().map((cell) => (
                    <div
                      key={cell.id}
                      className="px-4 py-2 flex items-center shrink-0"
                      style={{ width: cell.column.getSize() }}
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
