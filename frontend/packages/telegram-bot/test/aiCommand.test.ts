import { describe, it, expect, vi } from 'vitest';
import { aiCommand, parseAiPrompt } from '../src/commands/ai';
import * as openai from '@photobank/shared/api/openai';
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
    vi.spyOn(openai, 'createChatCompletion').mockRejectedValue(new Error('fail'));
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(sorryTryToRequestLaterMsg);
  });

  it('sends assistant message on success', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/ai test' } } as any;
    vi.spyOn(openai, 'createChatCompletion').mockResolvedValue({
      choices: [{ message: { role: 'assistant', content: 'hi' } }],
    } as any);
    await aiCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith('hi');
  });
});
