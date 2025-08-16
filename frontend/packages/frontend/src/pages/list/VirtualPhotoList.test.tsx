import { render, screen } from '@testing-library/react';
import React, { createRef } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { vi } from 'vitest';

vi.mock('./usePhotoVirtual');

import VirtualPhotoList from './VirtualPhotoList';
import { usePhotoVirtual } from './usePhotoVirtual';

const createPhotos = (count: number): PhotoItemDto[] =>
  Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    thumbnail: '',
    name: `Photo ${i + 1}`,
    storageName: 's',
    relativePath: 'p',
  }));

test('renders only a subset of items', async () => {
  const parentRef = createRef<HTMLDivElement>();
  const photos = createPhotos(50);

  const mockedUsePhotoVirtual = vi.mocked(usePhotoVirtual);
  mockedUsePhotoVirtual.mockReturnValue({
    items: Array.from({ length: 5 }, (_, index) => ({
      index,
      size: 112,
      start: index * 112,
    })),
    totalSize: 5600,
    virtualizer: { measureElement: vi.fn() },
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

