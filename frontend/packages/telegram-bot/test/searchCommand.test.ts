import { describe, it, expect, vi } from 'vitest';
import * as searchCommands from '../src/commands/search';
import * as photoService from '../src/services/photo';
import { i18n } from '../src/i18n';

describe('handleSearch', () => {
  it('replies with usage when caption missing', async () => {
    const ctx = {
      reply: vi.fn(),
      message: { text: '/search' },
      from: { id: 1 },
      t: (k: string) => i18n.t('en', k),
    } as any;
    await searchCommands.handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'search-usage'));
  });

  it('replies with fallback message on API failure', async () => {
    const ctx = {
      reply: vi.fn(),
      message: { text: '/search cats' },
      from: { id: 1 },
      t: (k: string) => i18n.t('en', k),
    } as any;
    vi.spyOn(photoService, 'searchPhotos').mockRejectedValue(new Error('fail'));
    await searchCommands.handleSearch(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'sorry-try-later'));
  });

  it('strips quotes around caption', async () => {
    const ctx = {
      reply: vi.fn(),
      message: { text: '/search "dog cat"' },
      from: { id: 1 },
      t: (k: string) => i18n.t('en', k),
    } as any;
    const searchSpy = vi
      .spyOn(photoService, 'searchPhotos')
      .mockResolvedValue({ totalCount: 0, items: [] } as any);

    await searchCommands.handleSearch(ctx);

    expect(searchSpy).toHaveBeenCalledWith(
      ctx,
      expect.objectContaining({ caption: 'dog cat' }),
    );
  });
});
