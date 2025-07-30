import { describe, it, expect, vi } from 'vitest';
import { aiCommand, parseAiPrompt, aiFilters } from '../src/commands/ai';
import * as openai from '@photobank/shared/ai/openai';
import * as dict from '@photobank/shared/dictionaries';
import * as photosApi from '@photobank/shared/generated';
import * as utils from '@photobank/shared/index';
import {
  aiCommandUsageMsg,
  aiFilterEmptyMsg,
  sorryTryToRequestLaterMsg,
  searchPhotosEmptyMsg,
} from '@photobank/shared/constants';

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
    const ctx = { reply: vi.fn(), message: { text: '/ai' } } as any;
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(aiCommandUsageMsg);
  });

  it('replies with fallback when API fails', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' } } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockRejectedValue(
      new Error('fail')
    );
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });

  it('requests photos with parsed filter', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' } } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockResolvedValue({
      persons: ['Alice'],
      tags: ['portrait'],
      dateFrom: new Date('2020-01-01T00:00:00Z'),
      dateTo: null,
    });
    vi.spyOn(dict, 'findBestPersonId').mockReturnValue(1);
    vi.spyOn(dict, 'findBestTagId').mockReturnValue(10);
    vi.spyOn(utils, 'getFilterHash').mockResolvedValue('hash');
    const searchSpy = vi
      .spyOn(photosApi.PhotosService, 'postApiPhotosSearch')
      .mockResolvedValue({ count: 0, photos: [] } as any);
    aiFilters.clear();

    await aiCommand(ctx);

    expect(aiFilters.has('hash')).toBe(true);
    expect(searchSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        persons: [1],
        tags: [10],
        takenDateFrom: '2020-01-01T00:00:00.000Z',
        top: 10,
        skip: 0,
      })
    );
    expect(ctx.reply).toHaveBeenCalledWith(searchPhotosEmptyMsg);
  });

  it('warns when filter is empty', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai empty' } } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockResolvedValue({
      persons: [],
      tags: [],
      dateFrom: null,
      dateTo: null,
    });
    const hashSpy = vi.spyOn(utils, 'getFilterHash');
    aiFilters.clear();

    await aiCommand(ctx);

    expect(hashSpy).not.toHaveBeenCalled();
    expect(aiFilters.size).toBe(0);
    expect(ctx.reply).toHaveBeenCalledWith(aiFilterEmptyMsg);
  });
});
