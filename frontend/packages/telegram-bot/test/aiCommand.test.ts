import { describe, it, expect, vi } from 'vitest';
import { aiCommand, parseAiPrompt, aiFilters } from '../src/commands/ai';
import * as openai from '@photobank/shared/ai/openai';
import * as photoService from '../src/services/photo';
import * as utils from '@photobank/shared/index';
import { i18n } from '../src/i18n';

describe('parseAiPrompt', () => {
  it('parses prompt from command', () => {
    expect(parseAiPrompt('/ai hello')).toBe('hello');
  });

  it('returns null without prompt', () => {
    expect(parseAiPrompt('/ai')).toBeNull();
  });
});

describe('aiCommand', () => {
  it('replies with usage on empty prompt', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai' }, t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'ai-usage'));
  });

  it('replies with fallback when API fails', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' }, t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockRejectedValue(
      new Error('fail')
    );
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'sorry-try-later'));
  });

  it('requests photos with parsed filter', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' }, t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockResolvedValue({
      personNames: ['Alice'],
      tagNames: ['portrait'],
      dateFrom: new Date('2020-01-01T00:00:00Z'),
      dateTo: null,
    });
    vi.spyOn(utils, 'getFilterHash').mockReturnValue('hash');
    const searchSpy = vi
      .spyOn(photoService, 'searchPhotos')
      .mockResolvedValue({ data: { count: 0, photos: [] } } as any);
    aiFilters.clear();

    await aiCommand(ctx);

    expect(aiFilters.has('hash')).toBe(true);
    expect(searchSpy).toHaveBeenCalledWith(
      ctx,
      expect.objectContaining({
        personNames: ['Alice'],
        tagNames: ['portrait'],
        takenDateFrom: '2020-01-01T00:00:00.000Z',
        top: 10,
        skip: 0,
      })
    );
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'search-photos-empty'));
  });

  it('warns when filter is empty', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai empty' }, t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockResolvedValue({
      personNames: [],
      tagNames: [],
      dateFrom: null,
      dateTo: null,
    });
    const hashSpy = vi.spyOn(utils, 'getFilterHash');
    aiFilters.clear();

    await aiCommand(ctx);

    expect(hashSpy).not.toHaveBeenCalled();
    expect(aiFilters.size).toBe(0);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'ai-filter-empty'));
  });

  it('accepts prompt override', async () => {
    const ctx = { reply: vi.fn(), message: { text: 'test' }, t: (k: string, p?: any) => i18n.t('en', k, p) } as any;
    const parseSpy = vi
      .spyOn(openai, 'parseQueryWithOpenAI')
      .mockResolvedValue({
        personNames: [],
        tagNames: [],
        dateFrom: null,
        dateTo: null,
      });
    vi.spyOn(utils, 'getFilterHash').mockResolvedValue('hash');
    vi.spyOn(photoService, 'searchPhotos').mockResolvedValue({
      data: { count: 0, photos: [] },
    } as any);
    aiFilters.clear();

    await aiCommand(ctx, 'cats');

    expect(parseSpy).toHaveBeenCalledWith('cats');
  });
});
