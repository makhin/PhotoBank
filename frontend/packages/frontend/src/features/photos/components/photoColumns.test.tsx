import * as React from 'react';
import { render, screen, renderHook } from '@testing-library/react';
import { compareAsc, parseISO } from 'date-fns';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

import { usePhotoColumns } from './photoColumns';

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

  it('renders taken date in DD.MM.YYYY format', () => {
    const { result } = renderHook(() => usePhotoColumns());
    const dateColumn = result.current.find((col) => col.id === 'date');
    expect(dateColumn).toBeDefined();
    const photo = createPhoto(1, '2024-06-07T10:15:00+03:00');
    const isoValue = dateColumn?.accessorFn?.(photo, 0) as string;
    const cellNode = dateColumn?.cell?.({ getValue: () => isoValue } as never);

    render(<>{cellNode}</>);

    expect(screen.getByText('07.06.2024')).toBeInTheDocument();
  });

  it('returns ISO strings that preserve chronological sorting', () => {
    const { result } = renderHook(() => usePhotoColumns());
    const dateColumn = result.current.find((col) => col.id === 'date');
    expect(dateColumn?.accessorFn).toBeDefined();

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
        const isoA = (dateColumn?.accessorFn?.(a, 0) as string) ?? '';
        const isoB = (dateColumn?.accessorFn?.(b, 0) as string) ?? '';
        return isoA.localeCompare(isoB);
      })
      .map((p) => p.id);

    expect(sortedByAccessor).toEqual(expectedOrder);
  });
});
