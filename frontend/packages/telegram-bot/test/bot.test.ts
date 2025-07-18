import { describe, it, expect, vi } from 'vitest';
import { handleThisDay } from '../src/commands/thisday';
import * as photosApi from '@photobank/shared/api/photos';

describe('handleThisDay', () => {
  it('replies with fallback message on API failure', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/thisday' } } as any;
    vi.spyOn(photosApi, 'searchPhotos').mockRejectedValue(new Error('fail'));
    await handleThisDay(ctx);
    expect(ctx.reply).toHaveBeenCalledWith('Sorry, try to request later.');
  });
});
