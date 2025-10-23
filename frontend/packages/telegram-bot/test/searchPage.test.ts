import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendSearchPage } from '../src/commands/search';
import * as photoService from '../src/services/photo';
import * as photo from '../src/photo';
import { i18n } from '../src/i18n';

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
      t: (k: string, p?: any) => i18n.t('en', k, p),
    } as any;
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      count: 1,
      photos: [basePhoto],
    } as any);

    await sendSearchPage(ctx, 'cats', 2, true);

    expect(photo.deletePhotoMessage).toHaveBeenCalled();
  });

  it('does not delete preview message when same page requested', async () => {
    const ctx = {
      chat: { id: 1 },
      reply: vi.fn(),
      editMessageText: vi.fn().mockResolvedValue(undefined),
      t: (k: string, p?: any) => i18n.t('en', k, p),
    } as any;
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      count: 1,
      photos: [basePhoto],
    } as any);

    await sendSearchPage(ctx, 'cats', 1, true);

    expect(photo.deletePhotoMessage).not.toHaveBeenCalled();
  });

  it('adds first and last navigation buttons', async () => {
    const ctx = { reply: vi.fn(), t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    const photos = Array.from({ length: 10 }, (_, i) => ({ ...basePhoto, id: i + 1 }));
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      count: 30,
      photos,
    } as any);

    await sendSearchPage(ctx, 'cats', 2, false);

    const [, opts] = ctx.reply.mock.calls[0];
    const navRow = opts.reply_markup.inline_keyboard.at(-1);
    const buttons = navRow.map((b: any) => b.text);
    expect(buttons).toEqual([
      i18n.t('en', 'first-page'),
      i18n.t('en', 'prev-page'),
      i18n.t('en', 'next-page'),
      i18n.t('en', 'last-page'),
    ]);
  });
});
