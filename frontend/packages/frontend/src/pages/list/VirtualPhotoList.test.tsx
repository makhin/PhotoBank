import { render, waitFor } from '@testing-library/react';
import React, { createRef } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import VirtualPhotoList from './VirtualPhotoList';

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
  const { container } = render(
    <div ref={parentRef} style={{ height: 400, overflow: 'auto' }}>
      <VirtualPhotoList
        photos={photos}
        parentRef={parentRef}
        renderRow={(p) => <div data-testid="row">{p.name}</div>}
      />
    </div>
  );

  await waitFor(() => {
    const rows = container.querySelectorAll('[data-testid="row"]');
    expect(rows.length).toBeGreaterThan(0);
    expect(rows.length).toBeLessThan(photos.length);
  });
});
