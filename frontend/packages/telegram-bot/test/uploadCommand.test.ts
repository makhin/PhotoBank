import { describe, it, expect, vi, beforeEach } from 'vitest';
import { uploadFailedMsg, uploadSuccessMsg } from '@photobank/shared/constants';

describe('uploadCommand', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
    process.env.BOT_TOKEN = 'token';
    process.env.API_EMAIL = 'e';
    process.env.API_PASSWORD = 'p';
  });

  it('replies with failure when no files provided', async () => {
    const adapter = await import('../src/fileAdapter');
    const { uploadCommand } = await import('../src/commands/upload');
    const ctx = { reply: vi.fn() } as any;
    const adapterSpy = vi.spyOn(adapter, 'getUploadFiles').mockResolvedValue([]);
    await uploadCommand(ctx);
    expect(adapterSpy).toHaveBeenCalledWith(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(uploadFailedMsg);
  });

  it('uploads files and replies success', async () => {
    const adapter = await import('../src/fileAdapter');
    const { uploadCommand } = await import('../src/commands/upload');
    const dictionaries = await import('../src/dictionaries');
    const photosApi = await import('@photobank/shared/generated');
    const ctx = { reply: vi.fn(), from: { username: 'user' } } as any;
    const fakeFile = {} as any;
    vi.spyOn(adapter, 'getUploadFiles').mockResolvedValue([fakeFile]);
    vi.spyOn(dictionaries, 'getStorageId').mockReturnValue(1);
    const uploadSpy = vi
      .spyOn(photosApi.PhotosService, 'postApiPhotosUpload')
      .mockResolvedValue(undefined as any);
    await uploadCommand(ctx);
    expect(uploadSpy).toHaveBeenCalledWith({
      files: [fakeFile],
      storageId: 1,
      path: 'user',
    });
    expect(ctx.reply).toHaveBeenCalledWith(uploadSuccessMsg);
  });
});
