import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import type { PersonMap, TagMap } from '@photobank/shared/metadata';

import PhotoListItemMobile from './PhotoListItemMobile';

const createMaps = (): { personsMap: PersonMap; tagsMap: TagMap } => ({
  personsMap: new Map(),
  tagsMap: new Map(),
});

describe('PhotoListItemMobile', () => {
  it('truncates long photo names to 10 characters with an ellipsis', () => {
    const longName = 'ABCDEFGHIJKLmnopqrstuvwxyz';
    const photo: PhotoItemDto = {
      id: 1,
      name: longName,
      captions: [],
      storageName: 'storage',
      relativePath: '/path/to/photo.jpg',
      thumbnailUrl: 'https://example.com/thumbnail.jpg',
      takenDate: new Date('2024-01-01T00:00:00Z'),
      isBW: false,
      isAdultContent: false,
      isRacyContent: false,
    };

    const { personsMap, tagsMap } = createMaps();

    render(
      <PhotoListItemMobile
        photo={photo}
        personsMap={personsMap}
        tagsMap={tagsMap}
        onClick={() => {}}
      />
    );

    const truncatedName = screen.getByText('ABCDEFGHIJ…');
    expect(truncatedName).toBeInTheDocument();
    expect(truncatedName).toHaveAttribute('title', longName);
  });
});
