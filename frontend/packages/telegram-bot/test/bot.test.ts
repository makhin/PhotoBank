import { describe, it, expect, vi } from 'vitest';
import { handleThisDay } from '../src/commands/thisday';
import * as photoService from '../src/services/photo';
import { i18n } from '../src/i18n';

describe('handleThisDay', () => {
  it('replies with fallback message on API failure', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/thisday' }, t: (k: string) => i18n.t('en', k) } as any;
    vi.spyOn(photoService, 'searchPhotos').mockRejectedValue(new Error('fail'));
    await handleThisDay(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'sorry-try-later'));
  });
});
