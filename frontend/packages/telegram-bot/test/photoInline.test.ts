import { it, expect, vi, beforeEach, afterEach } from 'vitest';
import { openPhotoInline, photoMessages, currentPagePhotos } from '../src/photo';
import * as photoService from '../src/services/photo';
import { i18n } from '../src/i18n';

const originalFetch = global.fetch;

const basePhoto = {
  id: 1,
  name: 'test',
  scale: 1,
  previewUrl: 'http://example.com/img.jpg',
  adultScore: 0,
  racyScore: 0,
  height: 100,
  width: 100,
};

beforeEach(() => {
  vi.restoreAllMocks();
  photoMessages.clear();
  currentPagePhotos.clear();
  global.fetch = vi.fn().mockResolvedValue({
    ok: true,
    arrayBuffer: async () => new ArrayBuffer(0),
  }) as unknown as typeof fetch;
});

afterEach(() => {
  vi.restoreAllMocks();
  if (originalFetch) {
    global.fetch = originalFetch;
  } else {
    // @ts-expect-error allow cleanup when fetch was undefined
    delete global.fetch;
  }
});

it('sends new message and stores id', async () => {
  vi.spyOn(photoService, 'getPhoto').mockResolvedValue(basePhoto as any);
  const ctx = {
    chat: { id: 1 },
    replyWithPhoto: vi.fn().mockResolvedValue({ message_id: 42 }),
    reply: vi.fn(),
    api: { editMessageMedia: vi.fn(), editMessageCaption: vi.fn() },
    t: (k: string, p?: any) => i18n.t('en', k, p),
  } as any;
  currentPagePhotos.set(1, { page: 1, ids: [1, 2] });
  await openPhotoInline(ctx, 1);
  expect(ctx.replyWithPhoto).toHaveBeenCalled();
  expect(photoMessages.get(1)).toBe(42);
  expect(ctx.replyWithPhoto.mock.calls[0][1].reply_markup).toBeDefined();
});

it('edits existing message when available', async () => {
  vi.spyOn(photoService, 'getPhoto').mockResolvedValue(basePhoto as any);
  photoMessages.set(1, 42);
  const ctx = {
    chat: { id: 1 },
    api: { editMessageMedia: vi.fn(), editMessageCaption: vi.fn() },
    replyWithPhoto: vi.fn().mockResolvedValue({ message_id: 43 }),
    reply: vi.fn(),
    t: (k: string, p?: any) => i18n.t('en', k, p),
  } as any;
  currentPagePhotos.set(1, { page: 1, ids: [1, 2] });
  await openPhotoInline(ctx, 1);
  expect(ctx.api.editMessageMedia).toHaveBeenCalled();
  expect(ctx.replyWithPhoto).not.toHaveBeenCalled();
  expect(ctx.api.editMessageMedia.mock.calls[0][3].reply_markup).toBeDefined();
});

it('adds navigation buttons from current page list', async () => {
  vi.spyOn(photoService, 'getPhoto').mockResolvedValue(basePhoto as any);
  const ctx = {
    chat: { id: 1 },
    replyWithPhoto: vi.fn().mockResolvedValue({ message_id: 1 }),
    reply: vi.fn(),
    api: { editMessageMedia: vi.fn(), editMessageCaption: vi.fn() },
    t: (k: string, p?: any) => i18n.t('en', k, p),
  } as any;
  currentPagePhotos.set(1, { page: 1, ids: [1, 2, 3] });
  await openPhotoInline(ctx, 2);
  const kb = ctx.replyWithPhoto.mock.calls[0][1].reply_markup.inline_keyboard;
  expect(kb[0][0].callback_data).toBe('photo_nav:1');
  expect(kb[0][1].callback_data).toBe('photo_nav:3');
});
