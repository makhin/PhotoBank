import { describe, it, expect, vi } from 'vitest';
import { aiCommand, parseAiPrompt } from '../src/commands/ai';
import * as openai from '@photobank/shared/ai/openai';
import * as dict from '@photobank/shared/dictionaries';
import { aiCommandUsageMsg, sorryTryToRequestLaterMsg } from '@photobank/shared/constants';

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
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockRejectedValue(new Error('fail'));
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });

  it('sends filter dto string on success', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' } } as any;
    vi.spyOn(openai, 'parseQueryWithOpenAI').mockResolvedValue({
      persons: ['Alice'],
      tags: ['portrait'],
      dateFrom: new Date('2020-01-01T00:00:00Z'),
      dateTo: null,
    });
    vi.spyOn(dict, 'findBestPersonId').mockReturnValue(1);
    vi.spyOn(dict, 'findBestTagId').mockReturnValue(10);
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(
      JSON.stringify({ persons: [1], tags: [10], takenDateFrom: '2020-01-01T00:00:00.000Z' }, null, 2)
    );
  });
});
