import { describe, it, expect, vi, beforeEach } from 'vitest';
import { openPhotoInline, photoMessages } from '../src/photo';
import * as photosApi from '@photobank/shared/api/photos';

const basePhoto = {
  id: 1,
  name: 'test',
  scale: 1,
  previewImage: Buffer.from('img').toString('base64'),
  adultScore: 0,
  racyScore: 0,
  height: 100,
  width: 100,
};

beforeEach(() => {
  photoMessages.clear();
  vi.restoreAllMocks();
});

it('sends new message and stores id', async () => {
  vi.spyOn(photosApi, 'getPhotoById').mockResolvedValue(basePhoto as any);
  const ctx = {
    chat: { id: 1 },
    replyWithPhoto: vi.fn().mockResolvedValue({ message_id: 42 }),
    reply: vi.fn(),
    api: { editMessageMedia: vi.fn(), editMessageCaption: vi.fn() },
  } as any;
  await openPhotoInline(ctx, 1);
  expect(ctx.replyWithPhoto).toHaveBeenCalled();
  expect(photoMessages.get(1)).toBe(42);
});

it('edits existing message when available', async () => {
  vi.spyOn(photosApi, 'getPhotoById').mockResolvedValue(basePhoto as any);
  photoMessages.set(1, 42);
  const ctx = {
    chat: { id: 1 },
    api: { editMessageMedia: vi.fn(), editMessageCaption: vi.fn() },
    replyWithPhoto: vi.fn().mockResolvedValue({ message_id: 43 }),
    reply: vi.fn(),
  } as any;
  await openPhotoInline(ctx, 1);
  expect(ctx.api.editMessageMedia).toHaveBeenCalled();
  expect(ctx.replyWithPhoto).not.toHaveBeenCalled();
});
