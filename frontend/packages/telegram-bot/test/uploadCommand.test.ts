import { describe, it, expect, vi } from 'vitest';
import axios from 'axios';
import * as shared from '@photobank/shared';
import * as dict from '../src/dictionaries';

vi.mock('../src/config', () => ({ BOT_TOKEN: 'token' }));

import { uploadCommand } from '../src/commands/upload';
import { uploadSuccessMsg } from '@photobank/shared/constants';

describe('uploadCommand', () => {
  it('uploads photo data via adapter', async () => {
    const ctx: any = {
      api: { getFile: vi.fn().mockResolvedValue({ file_path: 'photo.jpg' }) },
      message: { photo: [{}, { file_id: '123', file_unique_id: 'uniq123' }] },
      reply: vi.fn(),
      from: { username: 'john' },
    };

    vi.spyOn(dict, 'getStorageId').mockReturnValue(1);
    vi.spyOn(axios, 'get').mockResolvedValue({ data: new ArrayBuffer(0) });
    const uploadSpy = vi
      .spyOn(shared, 'uploadPhotosAdapter')
      .mockResolvedValue({} as any);

    await uploadCommand(ctx);

    expect(uploadSpy).toHaveBeenCalledWith({
      files: [expect.objectContaining({ name: 'uniq123.jpg' })],
      storageId: 1,
      path: 'john',
    });
    expect(ctx.reply).toHaveBeenCalledWith(uploadSuccessMsg);
  });
});

