import { describe, it, expect, vi } from 'vitest';
import { handleThisDay } from '../src/commands/thisday';
import * as photosApi from '@photobank/shared/generated';
import { sorryTryToRequestLaterMsg } from '@photobank/shared/constants';

describe('handleThisDay', () => {
  it('replies with fallback message on API failure', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/thisday' } } as any;
    vi.spyOn(photosApi.PhotosService, 'postApiPhotosSearch').mockRejectedValue(new Error('fail'));
    await handleThisDay(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });
});
