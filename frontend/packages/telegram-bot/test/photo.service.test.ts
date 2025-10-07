import type { Context } from 'grammy';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  photosUpload: vi.fn(),
  callWithContext: vi.fn(),
}));

vi.mock('../src/api/photobank/photos/photos', () => ({
  photosUpload: mocks.photosUpload,
  photosGetPhoto: vi.fn(),
  photosSearchPhotos: vi.fn(),
}));

vi.mock('../src/services/call-with-context', () => ({
  callWithContext: mocks.callWithContext,
}));

const { photosUpload, callWithContext } = mocks;

import { uploadPhotos } from '../src/services/photo';

describe('uploadPhotos', () => {
  const ctx = { from: { id: 99 } } as unknown as Context;

  beforeEach(() => {
    photosUpload.mockReset().mockResolvedValue({ data: null });
    callWithContext.mockReset().mockImplementation(async (_ctx, fn) => {
      return await (fn as () => Promise<unknown> | unknown)();
    });
  });

  it('normalizes file data before sending', async () => {
    const fileData = new Uint8Array([1, 2, 3]);

    await uploadPhotos(ctx, {
      files: [{ data: fileData, name: 'example.jpg' }],
      storageId: 7,
      path: 'albums/2024',
    });

    expect(callWithContext).toHaveBeenCalledWith(ctx, expect.any(Function));
    expect(photosUpload).toHaveBeenCalledTimes(1);

    const body = photosUpload.mock.calls[0]![0];
    expect(body.storageId).toBe(7);
    expect(body.path).toBe('albums/2024');

    const files = body.files as File[];
    expect(Array.isArray(files)).toBe(true);
    expect(files).toHaveLength(1);
    const [file] = files;
    expect(file).toBeInstanceOf(File);
    expect(file.name).toBe('example.jpg');
  });
});
