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

type ContextOverrides = {
  t?: ReturnType<typeof vi.fn>;
};

function createContext(overrides: ContextOverrides = {}): MyContext {
  const reply = vi.fn().mockResolvedValue({ message_id: 1 });
  const editMessageText = vi.fn().mockResolvedValue({});
  const defaultT = vi.fn((key: string, params?: Record<string, number>) => {
    if (key === 'unknown-year') return 'Unknown Year';
    if (key === 'page-info') return `Page ${params?.page} / ${params?.total}`;
    if (key === 'people-count') return `👥 ${params?.count}`;
    if (key === 'untitled') return 'Untitled';
    return key;
  });
  const t = overrides.t ?? defaultT;

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

  it('escapes HTML special characters from API data and translations', async () => {
    const customT = vi.fn((key: string, params?: Record<string, number>) => {
      if (key === 'unknown-year') return 'Unknown & <Year>';
      if (key === 'page-info') return `Page <${params?.page}> & ${params?.total}`;
      if (key === 'people-count') return `👥 ${params?.count} & friends`;
      if (key === 'untitled') return 'Untitled & <Photo>';
      return key;
    });
    const ctx = createContext({ t: customT });
    searchPhotosMock.mockResolvedValue({
      totalCount: 1,
      items: [
        createPhoto({
          id: 99,
          name: '',
          storageName: 'Main & Storage',
          relativePath: "album/it's<ok>.jpg",
          takenDate: new Date('not-a-date'),
          captions: ['<b>bold & fun</b> "caption"'],
          persons: ([{}] as unknown) as PhotoItemDto['persons'],
          isAdultContent: true,
        }),
      ],
    });

    await sendPhotosPage({
      ctx,
      filter: {} as FilterDto,
      page: 1,
      fallbackMessage: 'No photos found',
      buildCallbackData: (page) => `page:${page}`,
    });

    const replyMock = ctx.reply as unknown as ReturnType<typeof vi.fn>;
    expect(replyMock).toHaveBeenCalled();
    const [[text, options]] = replyMock.mock.calls as [[string, { parse_mode?: string }]];

    expect(options?.parse_mode).toBe('HTML');
    expect(text).toContain('📅 <b>Unknown &amp; &lt;Year&gt;</b>');
    expect(text).toContain('📁 Main &amp; Storage / album/it&#39;s&lt;ok&gt;.jpg');
    expect(text).toContain('[1] <b>Untitled &amp; &lt;Photo&gt;</b>');
    expect(text).toContain('&lt;b&gt;bold &amp; fun&lt;/b&gt; &quot;caption&quot;');
    expect(text).toContain('👥 1 &amp; friends');
    expect(text).toContain('🔞');
    expect(text).toContain('Page &lt;1&gt; &amp; 1');
    expect(text).not.toContain('<b>bold & fun</b>');
  });
});
