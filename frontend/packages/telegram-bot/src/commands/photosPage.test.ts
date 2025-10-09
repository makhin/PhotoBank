import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { FilterDto } from '@photobank/shared';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

vi.mock('../services/photo', () => ({
  searchPhotos: vi.fn(),
}));

vi.mock('../errorHandler', () => ({
  handleCommandError: vi.fn(),
}));

vi.mock('../photo', async () => {
  const actual = await vi.importActual<typeof import('../photo')>('../photo');
  return {
    ...actual,
    deletePhotoMessage: vi.fn(),
  };
});

import { DEFAULT_PHOTO_FILTER } from '@photobank/shared';

import { sendPhotosPage } from './photosPage';
import type { MyContext } from '../i18n';
import { captionCache, currentPagePhotos, deletePhotoMessage, photoMessages } from '../photo';
import { searchPhotos } from '../services/photo';

const searchPhotosMock = vi.mocked(searchPhotos);

type PhotoOverrides = Partial<PhotoItemDto>;

function createPhoto(overrides: PhotoOverrides = {}): PhotoItemDto {
  return {
    id: 1,
    name: 'Sample Photo Name',
    storageName: 'Main Storage',
    relativePath: 'album/photo.jpg',
    takenDate: new Date('2024-05-01T10:00:00Z'),
    captions: ['A quick caption'],
    persons: [],
    isAdultContent: false,
    isRacyContent: false,
    ...overrides,
  } satisfies PhotoItemDto;
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
    photoMessages.clear();
  });

  it('renders the year for a valid ISO takenDate', async () => {
    const ctx = createContext();
    searchPhotosMock.mockResolvedValue({
      totalCount: 1,
      items: [createPhoto({ takenDate: new Date('2023-08-15T00:00:00Z') })],
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
      expect.objectContaining({ page: 1, pageSize: DEFAULT_PHOTO_FILTER.pageSize })
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
      items: [createPhoto({ takenDate: new Date('not-a-date') })],
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

  it('deletes any previous photo preview when starting a fresh search', async () => {
    const ctx = createContext();
    const chatId = ctx.chat?.id ?? 0;

    currentPagePhotos.set(chatId, { page: 2, ids: [42] });
    photoMessages.set(chatId, 101);

    const deletePhotoMessageMock = vi.mocked(deletePhotoMessage);

    searchPhotosMock.mockResolvedValue({
      totalCount: 1,
      items: [createPhoto({ id: 42 })],
    });

    await sendPhotosPage({
      ctx,
      filter: {} as FilterDto,
      page: 1,
      fallbackMessage: 'No photos found',
      buildCallbackData: (page) => `page:${page}`,
    });

    expect(deletePhotoMessageMock).toHaveBeenCalledWith(ctx);
    expect(ctx.reply).toHaveBeenCalled();
  });
});
