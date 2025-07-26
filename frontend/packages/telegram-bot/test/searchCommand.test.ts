import { describe, it, expect, vi } from 'vitest';
import { handleSearch } from '../src/commands/search';
import * as photosApi from '@photobank/shared/api/photos';
import { sorryTryToRequestLaterMsg, searchCommandUsageMsg } from '@photobank/shared/constants';

describe('handleSearch', () => {
  it('replies with usage when caption missing', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/search' } } as any;
    await handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(searchCommandUsageMsg);
  });

  it('replies with fallback message on API failure', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/search cats' } } as any;
    vi.spyOn(photosApi, 'searchPhotos').mockRejectedValue(new Error('fail'));
    await handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });
});
