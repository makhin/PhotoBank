import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { FilterDto } from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

vi.mock('../services/photo', () => ({
  searchPhotos: vi.fn(),
}));

vi.mock('../errorHandler', () => ({
  handleCommandError: vi.fn(),
}));

import { sendPhotosPage, PHOTOS_PAGE_SIZE } from './photosPage';
import type { MyContext } from '../i18n';
import { captionCache, currentPagePhotos } from '../photo';
import { searchPhotos } from '../services/photo';

const searchPhotosMock = vi.mocked(searchPhotos);

type PhotoOverrides = Partial<Omit<PhotoItemDto, 'takenDate'>> & {
  takenDate?: string | Date | null;
};

function createPhoto(overrides: PhotoOverrides = {}): PhotoItemDto {
  return {
    id: 1,
    name: 'Sample Photo Name',
    storageName: 'Main Storage',
    relativePath: 'album/photo.jpg',
    takenDate: '2024-05-01T10:00:00Z',
    captions: ['A quick caption'],
    persons: [],
    isAdultContent: false,
    isRacyContent: false,
    ...overrides,
  } as unknown as PhotoItemDto;
}

function createContext(): MyContext {
  const reply = vi.fn().mockResolvedValue({ message_id: 1 });
  const editMessageText = vi.fn().mockResolvedValue({});
  const t = vi.fn((key: string, params?: Record<string, number>) => {
    if (key === 'unknown-year') return 'Unknown Year';
    if (key === 'page-info') return `Page ${params?.page} / ${params?.total}`;
    if (key === 'people-count') return `👥 ${params?.count}`;
    if (key === 'untitled') return 'Untitled';
    return key;
  });

  return {
    chat: { id: 123 },
    reply,
    editMessageText,
    t,
  } as unknown as MyContext;
}

describe('sendPhotosPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    captionCache.clear();
    currentPagePhotos.clear();
  });

  it('renders the year for a valid ISO takenDate', async () => {
    const ctx = createContext();
    searchPhotosMock.mockResolvedValue({
      totalCount: 1,
      items: [createPhoto({ takenDate: '2023-08-15T00:00:00Z' })],
    });

    await sendPhotosPage({
      ctx,
      filter: {} as FilterDto,
      page: 1,
      fallbackMessage: 'No photos found',
      buildCallbackData: (page) => `page:${page}`,
    });

    expect(searchPhotosMock).toHaveBeenCalledWith(
      ctx,
      expect.objectContaining({ page: 1, pageSize: PHOTOS_PAGE_SIZE })
    );
    expect(ctx.reply).toHaveBeenCalledWith(
      expect.stringContaining('<b>2023</b>'),
      expect.objectContaining({ parse_mode: 'HTML' })
    );
  });

  it('falls back to unknown year when takenDate is invalid', async () => {
    const ctx = createContext();
    searchPhotosMock.mockResolvedValue({
      totalCount: 1,
      items: [createPhoto({ takenDate: 'not-a-date' })],
    });

    await sendPhotosPage({
      ctx,
      filter: {} as FilterDto,
      page: 1,
      fallbackMessage: 'No photos found',
      buildCallbackData: (page) => `page:${page}`,
    });

    expect(ctx.reply).toHaveBeenCalledWith(
      expect.stringContaining('<b>Unknown Year</b>'),
      expect.objectContaining({ parse_mode: 'HTML' })
    );
  });
});
