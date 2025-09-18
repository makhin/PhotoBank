import { describe, it, expect, vi, afterEach } from 'vitest';
import { InputFile } from 'grammy';
import type { PhotoDto } from '@photobank/shared/api/photobank';

import { formatPhotoMessage } from './formatPhotoMessage';
import { loadPhotoFile } from './photo';

function createPhoto(overrides: Partial<PhotoDto> = {}): PhotoDto {
  return {
    id: 1,
    name: 'Sample Photo',
    ...overrides,
  } as PhotoDto;
}

describe('formatPhotoMessage', () => {
  it('formats ISO string takenDate values without throwing', () => {
    const isoString = '2023-02-01T15:30:00.000Z';
    const photo = createPhoto({
      takenDate: isoString as unknown as Date,
    });

    let result: ReturnType<typeof formatPhotoMessage> | undefined;
    expect(() => {
      result = formatPhotoMessage(photo);
    }).not.toThrow();

    expect(result?.caption).toContain('📅 01.02.2023');
  });
});

describe('loadPhotoFile', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('returns an InputFile when preview download succeeds', async () => {
    const bytes = new Uint8Array([1, 2, 3]);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockImplementation(async () => new Response(bytes, { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await loadPhotoFile(
      createPhoto({ previewUrl: 'https://example.com/photo.jpg' })
    );

    expect(fetchMock).toHaveBeenCalledWith('https://example.com/photo.jpg');
    expect(result.photoFile).toBeInstanceOf(InputFile);
    expect(result.caption).toContain('📸');
    expect(result.hasSpoiler).toBe(false);
  });

  it('derives filename from photo name when missing extension', async () => {
    const bytes = new Uint8Array([4, 5, 6]);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockImplementation(async () => new Response(bytes, { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await loadPhotoFile(
      createPhoto({
        name: 'Holiday Shot',
        previewUrl: 'https://example.com/path/pic.png?token=abc',
      })
    );

    expect(result.photoFile?.filename).toBe('Holiday Shot.png');
  });

  it('falls back to caption when fetch fails', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockImplementation(async () => new Response(null, { status: 404 }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await loadPhotoFile(
      createPhoto({ previewUrl: 'https://example.com/missing.jpg' })
    );

    expect(result.photoFile).toBeUndefined();
    expect(result.caption).toContain('📸');
  });

  it('skips downloading when preview is unavailable', async () => {
    const fetchMock = vi.fn<typeof fetch>();
    vi.stubGlobal('fetch', fetchMock);

    const result = await loadPhotoFile(createPhoto({ previewUrl: null }));

    expect(fetchMock).not.toHaveBeenCalled();
    expect(result.photoFile).toBeUndefined();
  });
});
