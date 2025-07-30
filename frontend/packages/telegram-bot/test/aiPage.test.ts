import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendAiPage, aiFilters } from '../src/commands/ai';
import * as photosApi from '@photobank/shared/generated';
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
  aiFilters.clear();
});

describe('sendAiPage', () => {
  it('deletes preview message when page changes', async () => {
    const ctx = {
      chat: { id: 1 },
      reply: vi.fn(),
      editMessageText: vi.fn().mockResolvedValue(undefined),
    } as any;
    aiFilters.set('hash', {} as any);
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photosApi.PhotosService, 'postApiPhotosSearch').mockResolvedValue({
      count: 1,
      photos: [basePhoto],
    } as any);

    await sendAiPage(ctx, 'hash', 2, true);

    expect(photo.deletePhotoMessage).toHaveBeenCalled();
  });

  it('does not delete preview message when same page requested', async () => {
    const ctx = {
      chat: { id: 1 },
      reply: vi.fn(),
      editMessageText: vi.fn().mockResolvedValue(undefined),
    } as any;
    aiFilters.set('hash', {} as any);
    photo.currentPagePhotos.set(1, { page: 1, ids: [1] });
    vi.spyOn(photo, 'deletePhotoMessage').mockResolvedValue();
    vi.spyOn(photosApi.PhotosService, 'postApiPhotosSearch').mockResolvedValue({
      count: 1,
      photos: [basePhoto],
    } as any);

    await sendAiPage(ctx, 'hash', 1, true);

    expect(photo.deletePhotoMessage).not.toHaveBeenCalled();
  });
});

