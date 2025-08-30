import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendPhotoSmart, sendAlbumSmart } from '../src/telegram/send';
import { getFileId, setFileId, delFileId } from '../src/cache/fileIdCache';
import type { PhotoItemDto } from '../src/types';
import { limiter } from '../src/utils/limiter';

const basePhoto: PhotoItemDto = {
  id: 1,
  name: 'Test',
  takenDate: '2024-01-01',
  previewUrl: 'http://example.com/prev.jpg',
  storageName: 's',
  relativePath: 'r',
};

beforeEach(() => {
  limiter.updateSettings({ minTime: 0 });
});

describe('sendPhotoSmart', () => {
  it('stores file_id in cache after successful send', async () => {
    const ctx: any = {
      chat: { id: 1 },
      api: { sendPhoto: vi.fn().mockResolvedValue({ photo: [{ file_id: 'abc' }] }) },
    };
    delFileId(basePhoto.id);
    await sendPhotoSmart(ctx, basePhoto);
    expect(ctx.api.sendPhoto).toHaveBeenCalledWith(
      ctx.chat.id,
      basePhoto.previewUrl,
      { caption: expect.any(String) },
    );
    expect(ctx.api.sendPhoto).toHaveBeenCalledTimes(1);
    expect(getFileId(basePhoto.id)).toBe('abc');
  });

  it('invalidates cache when Telegram reports wrong file identifier', async () => {
    const photo = { ...basePhoto, id: 2 };
    setFileId(photo.id, 'old');
    const err = { error_code: 400, description: 'wrong file identifier' };
    const ctx: any = {
      chat: { id: 1 },
      api: {
        sendPhoto: vi
          .fn()
          .mockRejectedValueOnce(err)
          .mockResolvedValue({ photo: [{ file_id: 'new' }] }),
      },
    };
    await sendPhotoSmart(ctx, photo);
    expect(ctx.api.sendPhoto).toHaveBeenNthCalledWith(
      1,
      ctx.chat.id,
      'old',
      { caption: expect.any(String) },
    );
    expect(ctx.api.sendPhoto).toHaveBeenNthCalledWith(
      2,
      ctx.chat.id,
      photo.previewUrl,
      { caption: expect.any(String) },
    );
    expect(ctx.api.sendPhoto).toHaveBeenCalledTimes(2);
    expect(getFileId(photo.id)).toBe('new');
  });
});

describe('sendAlbumSmart', () => {
  it('falls back to single sends when mediaGroup fails', async () => {
    const photos: PhotoItemDto[] = [
      { ...basePhoto, id: 10 },
      { ...basePhoto, id: 11 },
    ];
    const ctx: any = {
      chat: { id: 1 },
      api: {
        sendMediaGroup: vi.fn().mockRejectedValue(new Error('fail')),
        sendPhoto: vi.fn().mockResolvedValue({ photo: [{ file_id: 'x' }] }),
      },
    };
    await sendAlbumSmart(ctx, photos);
    expect(ctx.api.sendMediaGroup).toHaveBeenCalledTimes(1);
    expect(ctx.api.sendPhoto).toHaveBeenCalledTimes(photos.length);
  });
});
