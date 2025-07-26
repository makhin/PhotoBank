import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendSearchPage } from '../src/commands/search';
import * as photosApi from '@photobank/shared/api/photos';
import * as photo from '../src/photo';

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
    vi.spyOn(photosApi, 'searchPhotos').mockResolvedValue({ count: 1, photos: [basePhoto] } as any);

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
    vi.spyOn(photosApi, 'searchPhotos').mockResolvedValue({ count: 1, photos: [basePhoto] } as any);

    await sendSearchPage(ctx, 'cats', 1, true);

    expect(photo.deletePhotoMessage).not.toHaveBeenCalled();
  });
});
