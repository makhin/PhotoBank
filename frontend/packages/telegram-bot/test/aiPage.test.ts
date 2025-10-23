import { describe, it, expect, vi, beforeEach } from 'vitest';
import { sendAiPage } from '../src/commands/ai';
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

describe('sendAiPage', () => {
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
      totalCount: 1,
      items: [basePhoto],
    } as any);

    await sendAiPage(ctx, {} as any, 2, true);

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
      totalCount: 1,
      items: [basePhoto],
    } as any);

    await sendAiPage(ctx, {} as any, 1, true);

    expect(photo.deletePhotoMessage).not.toHaveBeenCalled();
  });
});

