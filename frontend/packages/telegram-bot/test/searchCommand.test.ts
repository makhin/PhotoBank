import { describe, it, expect, vi } from 'vitest';
import * as searchCommands from '../src/commands/search';
import * as photoService from '../src/services/photo';
import { sorryTryToRequestLaterMsg, searchCommandUsageMsg } from '@photobank/shared/constants';

describe('handleSearch', () => {
  it('replies with usage when caption missing', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/search' } } as any;
    await searchCommands.handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(searchCommandUsageMsg);
  });

  it('replies with fallback message on API failure', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/search cats' } } as any;
    vi.spyOn(photoService, 'searchPhotos').mockRejectedValue(new Error('fail'));
    await searchCommands.handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });

  it('strips quotes around caption', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/search "dog cat"' } } as any;
    const searchSpy = vi
      .spyOn(photoService, 'searchPhotos')
      .mockResolvedValue({ data: { count: 0, photos: [] } } as any);

    await searchCommands.handleSearch(ctx);

    expect(searchSpy).toHaveBeenCalledWith(
      expect.objectContaining({ caption: 'dog cat' }),
    );
  });
});
