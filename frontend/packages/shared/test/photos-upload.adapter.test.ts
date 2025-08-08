import { describe, it, expect, vi } from 'vitest';
import axios, { type AxiosResponse } from 'axios';

import { OpenAPI } from '../src/generated';
import { uploadPhotosAdapter } from '../src/adapters/photos-upload.adapter';

describe('uploadPhotosAdapter', () => {
  it('adds Authorization header when OpenAPI.TOKEN is resolver', async () => {
    OpenAPI.TOKEN = () => Promise.resolve('token123');
    const post = vi
      .spyOn(axios, 'post')
      .mockResolvedValue({} as unknown as AxiosResponse);

    await uploadPhotosAdapter({
      files: [{ buffer: Buffer.from('data'), name: 'a.txt' }],
      storageId: 1,
      path: 'user',
    });

    expect(post).toHaveBeenCalled();
    const headers = post.mock.calls[0][2]?.headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer token123');
  });
});
