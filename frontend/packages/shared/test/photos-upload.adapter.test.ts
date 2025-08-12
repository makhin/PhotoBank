import { describe, it, expect, vi } from 'vitest';
import { configureApi, configureApiAuth } from '../src/api/photobank/fetcher';
import { uploadPhotosAdapter } from '../src/adapters/photos-upload.adapter';

describe('uploadPhotosAdapter', () => {
  it('adds Authorization header when token provider is set', async () => {
    configureApi('http://api');
    configureApiAuth(() => 'token123');
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers(),
      json: async () => null,
    } as any);
    // @ts-ignore
    global.fetch = fetchMock;
    await uploadPhotosAdapter({
      files: [{ buffer: Buffer.from('data'), name: 'a.txt' }],
      storageId: 1,
      path: 'user',
    });
    expect(fetchMock).toHaveBeenCalled();
    const [, options] = fetchMock.mock.calls[0];
    expect(options.headers.get('Authorization')).toBe('Bearer token123');
  });
});
