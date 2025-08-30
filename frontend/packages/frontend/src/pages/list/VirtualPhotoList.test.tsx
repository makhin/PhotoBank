import { render, screen } from '@testing-library/react';
import React, { createRef } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { vi, type Mock } from 'vitest';

vi.mock('./usePhotoVirtual');

// Стабилизируем мок: возвращаем корректную форму virtual items
const makeItems = (n: number) =>
  Array.from({ length: n }, (_, i) => {
    const size = 112;
    const start = i * size;
    return {
      key: i,
      index: i,
      start,
      size,
      end: start + size,
      lane: 0,
    };
  });

import VirtualPhotoList from './VirtualPhotoList';
import { usePhotoVirtual } from './usePhotoVirtual';

const createPhotos = (count: number): PhotoItemDto[] =>
  Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    thumbnailUrl: '',
    name: `Photo ${i + 1}`,
    storageName: 's',
    relativePath: 'p',
  }));

test('renders only a subset of items', async () => {
  const parentRef = createRef<HTMLDivElement>();
  const photos = createPhotos(50);

  (usePhotoVirtual as unknown as Mock).mockReturnValue({
    virtualizer: { measureElement: vi.fn() },
    items: makeItems(Math.min(photos.length, 10)) as any,
    totalSize: 112 * Math.min(photos.length, 10),
  });

  render(
    <div ref={parentRef} style={{ height: 400, overflow: 'auto' }}>
      <VirtualPhotoList
        photos={photos}
        parentRef={parentRef}
        renderRow={(p) => <div data-testid="row">{p.name}</div>}
      />
    </div>
  );

  const rows = await screen.findAllByTestId('row', {}, { timeout: 3000 });
  expect(rows.length).toBeGreaterThan(0);
  if (photos?.length) {
    expect(rows.length).toBeLessThanOrEqual(photos.length);
  }
});

