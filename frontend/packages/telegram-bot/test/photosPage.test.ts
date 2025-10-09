import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MyContext } from '../src/i18n';
import { DEFAULT_PHOTO_FILTER } from '@photobank/shared';

import { sendPhotosPage } from '../src/commands/photosPage';

const searchPhotos = vi.hoisted(() => vi.fn());
const handleCommandError = vi.hoisted(() => vi.fn());
const deletePhotoMessage = vi.hoisted(() => vi.fn(() => Promise.resolve()));
const captionCache = vi.hoisted(() => new Map<number, string>());
const currentPagePhotos = vi.hoisted(
  () => new Map<number, { page: number; ids: number[] }>(),
);
const setLastFilter = vi.hoisted(() => vi.fn());

vi.mock('../src/services/photo', () => ({
  searchPhotos: (...args: unknown[]) => searchPhotos(...args),
}));

vi.mock('../src/errorHandler', () => ({
  handleCommandError: (...args: unknown[]) => handleCommandError(...args),
}));

vi.mock('../src/photo', () => ({
  captionCache,
  currentPagePhotos,
  deletePhotoMessage,
}));

vi.mock('../src/cache/lastFilterCache', () => ({
  setLastFilter: (...args: unknown[]) => setLastFilter(...args),
}));

describe('sendPhotosPage', () => {
  beforeEach(() => {
    searchPhotos.mockReset();
    handleCommandError.mockReset();
    deletePhotoMessage.mockClear();
    captionCache.clear();
    currentPagePhotos.clear();
    setLastFilter.mockReset();
  });

  it('saves last filter with pagination when source is provided', async () => {
    const ctx = {
      chat: { id: 42 },
      reply: vi.fn(() => Promise.resolve()),
      t: vi.fn((key: string) => key),
    } as unknown as MyContext;

    searchPhotos.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          id: 1,
          name: 'example',
          storageName: 'storage',
          relativePath: 'path',
          captions: ['caption'],
          persons: [],
          isAdultContent: false,
          isRacyContent: false,
          takenDate: '2024-01-01T00:00:00Z',
        },
      ],
    });

    await sendPhotosPage({
      ctx,
      filter: { caption: 'sunset' },
      page: 2,
      fallbackMessage: 'fallback',
      buildCallbackData: (page) => `cb:${page}`,
      saveLastFilterSource: 'search',
    });

    const expectedFilter = {
      caption: 'sunset',
      page: 2,
      pageSize: DEFAULT_PHOTO_FILTER.pageSize,
    };

    expect(setLastFilter).toHaveBeenCalledWith(42, expectedFilter, 'search');
    expect(searchPhotos).toHaveBeenCalledWith(ctx, expectedFilter);
  });
});
