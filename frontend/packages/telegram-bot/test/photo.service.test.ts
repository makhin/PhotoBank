import type { Context } from 'grammy';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const request = vi.fn();
  return {
    ensureUserAccessToken: vi.fn<
      [Context, boolean | undefined],
      Promise<string>
    >(),
    invalidateUserToken: vi.fn<
      [Context | { from?: { id?: number } }],
      void
    >(),
    request,
    create: vi.fn(() => ({ request })),
    isAxiosError: vi.fn(() => false),
  };
});

vi.mock('@/auth', () => ({
  ensureUserAccessToken: mocks.ensureUserAccessToken,
  invalidateUserToken: mocks.invalidateUserToken,
}));

vi.mock('axios', () => ({
  default: {
    create: mocks.create,
    isAxiosError: mocks.isAxiosError,
  },
  create: mocks.create,
  isAxiosError: mocks.isAxiosError,
}));

const { ensureUserAccessToken, request } = mocks;

import { uploadPhotos } from '../src/services/photo';

describe('uploadPhotos', () => {
  const ctx = { from: { id: 99 } } as unknown as Context;

  beforeEach(() => {
    ensureUserAccessToken.mockReset();
    request.mockReset();
  });

  it('sends files via photobankAxios with bearer authorization', async () => {
    ensureUserAccessToken.mockResolvedValue('token-abc');
    request.mockResolvedValue({ data: null });

    await uploadPhotos(ctx, {
      files: [{ data: new Uint8Array([1, 2, 3]), name: 'example.jpg' }],
      storageId: 7,
      path: 'albums/2024',
    });

    expect(ensureUserAccessToken).toHaveBeenCalledTimes(1);
    expect(ensureUserAccessToken).toHaveBeenCalledWith(ctx, false);
    expect(request).toHaveBeenCalledTimes(1);

    const config = request.mock.calls[0][0];
    expect(config.method).toBe('POST');
    expect(config.url).toBe('/photos/upload');
    expect(config.headers?.Authorization).toBe('Bearer token-abc');

    const formData = config.data as FormData;
    expect(formData).toBeInstanceOf(FormData);
    expect(formData.get('storageId')).toBe('7');
    expect(formData.get('path')).toBe('albums/2024');

    const files = formData.getAll('files');
    expect(files).toHaveLength(1);
    const [file] = files;
    expect(file).toBeInstanceOf(File);
    expect((file as File).name).toBe('example.jpg');
  });
});
