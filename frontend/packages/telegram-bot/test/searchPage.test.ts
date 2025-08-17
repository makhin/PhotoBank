import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendSearchPage } from '../src/commands/search';
import * as photoService from '../src/services/photo';
import * as photo from '../src/photo';
import {
  firstPageText,
  prevPageText,
  nextPageText,
  lastPageText,
} from '@photobank/shared/constants';

const basePhoto = {
  id: 1,
  name: 'test',
  storageName: 's',
  relativePath: 'p',
  takenDate: new Date().toISOString(),
  persons: [],
  isAdultContent: false,
  isRacyContent: false,
  captions: [],
};

beforeEach(() => {
  vi.restoreAllMocks();
  photo.currentPagePhotos.clear();
});

describe('sendSearchPage', () => {
  it('deletes preview message when page changes', async () => {
    const ctx = {
      chat: { id: 1 },
      reply: vi.fn(),
      editMessageText: vi.fn().mockResolvedValue(undefined),
    } as any;
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      data: { count: 1, photos: [basePhoto] },
    } as any);

    await sendSearchPage(ctx, 'cats', 2, true);

    expect(photo.deletePhotoMessage).toHaveBeenCalled();
  });

  it('does not delete preview message when same page requested', async () => {
    const ctx = {
      chat: { id: 1 },
      reply: vi.fn(),
      editMessageText: vi.fn().mockResolvedValue(undefined),
    } as any;
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      data: { count: 1, photos: [basePhoto] },
    } as any);

    await sendSearchPage(ctx, 'cats', 1, true);

    expect(photo.deletePhotoMessage).not.toHaveBeenCalled();
  });

  it('adds first and last navigation buttons', async () => {
    const ctx = { reply: vi.fn() } as any;
    const photos = Array.from({ length: 10 }, (_, i) => ({ ...basePhoto, id: i + 1 }));
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      data: { count: 30, photos },
    } as any);

    await sendSearchPage(ctx, 'cats', 2, false);

    const [, opts] = ctx.reply.mock.calls[0];
    const navRow = opts.reply_markup.inline_keyboard.at(-1);
    const buttons = navRow.map((b: any) => b.text);
    expect(buttons).toEqual([
      firstPageText,
      prevPageText,
      nextPageText,
      lastPageText,
    ]);
  });
});
