import * as React from 'react';
import { render, screen, renderHook } from '@testing-library/react';
import { compareAsc, format, parseISO } from 'date-fns';
import type {
  AccessorFnColumnDef,
  CellContext,
  ColumnDef,
} from '@tanstack/react-table';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import '@testing-library/jest-dom';

import { usePhotoColumns } from './photoColumns';

const hasAccessorFn = (
  column: ColumnDef<PhotoItemDto>,
): column is AccessorFnColumnDef<PhotoItemDto, unknown> =>
  'accessorFn' in column && typeof column.accessorFn === 'function';

const hasCellRenderer = (
  column: AccessorFnColumnDef<PhotoItemDto, unknown>,
): column is AccessorFnColumnDef<PhotoItemDto, unknown> & {
  cell: (context: CellContext<PhotoItemDto, unknown>) => React.ReactNode;
} => typeof column.cell === 'function';

type MockRootState = {
  metadata: {
    loaded: boolean;
    persons: { id: number; name: string }[];
    tags: { id: number; name: string }[];
  };
};

let mockState: MockRootState;

vi.mock('@/app/hook', () => ({
  useAppSelector: (selector: (state: MockRootState) => unknown) => selector(mockState),
}));

const createPhoto = (id: number, takenDate?: string): PhotoItemDto => ({
  id,
  name: `Photo ${id}`,
  storageName: 'storage',
  relativePath: `photos/${id}.jpg`,
  ...(takenDate ? { takenDate: takenDate as unknown as Date } : {}),
});

describe('usePhotoColumns - date column', () => {
  beforeEach(() => {
    mockState = {
      metadata: {
        loaded: true,
        persons: [],
        tags: [],
      },
    };
  });

  it('renders taken date in DD.MM.YYYY hh:mm format', () => {
    const { result } = renderHook(() => usePhotoColumns());
    const dateColumn = result.current.find((column) => column.id === 'date');
    expect(dateColumn).toBeDefined();
    if (!dateColumn || !hasAccessorFn(dateColumn) || !hasCellRenderer(dateColumn)) {
      return;
    }
    const photo = createPhoto(1, '2024-06-07T10:15:00+03:00');
    const timestamp = dateColumn.accessorFn(photo, 0) as number;
    const cellNode = dateColumn.cell({ getValue: () => timestamp } as never);

    render(<>{cellNode}</>);

    expect(
      screen.getByText(
        format(parseISO('2024-06-07T10:15:00+03:00'), 'dd.MM.yyyy hh:mm'),
      ),
    ).toBeInTheDocument();
  });

  it('returns timestamps that preserve chronological sorting', () => {
    const { result } = renderHook(() => usePhotoColumns());
    const dateColumn = result.current.find((column) => column.id === 'date');
    expect(dateColumn).toBeDefined();
    if (!dateColumn || !hasAccessorFn(dateColumn) || !hasCellRenderer(dateColumn)) {
      return;
    }

    const photos = [
      createPhoto(1, '2024-06-07T10:15:00+03:00'),
      createPhoto(2, '2024-06-07T07:00:00Z'),
      createPhoto(3, '2023-12-31T23:30:00-02:00'),
    ];

    const expectedOrder = photos
      .slice()
      .sort((a, b) =>
        compareAsc(
          parseISO((a.takenDate as unknown as string) ?? ''),
          parseISO((b.takenDate as unknown as string) ?? ''),
        ),
      )
      .map((p) => p.id);

    const sortedByAccessor = photos
      .slice()
      .sort((a, b) => {
        const tsA = (dateColumn.accessorFn(a, 0) as number | null) ?? 0;
        const tsB = (dateColumn.accessorFn(b, 0) as number | null) ?? 0;
        return tsA - tsB;
      })
      .map((p) => p.id);

    expect(sortedByAccessor).toEqual(expectedOrder);
  });
});