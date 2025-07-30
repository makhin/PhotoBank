import { describe, it, expect, vi } from 'vitest';
import { helpCommand } from '../src/commands/help';
import { helpBotMsg } from '@photobank/shared/constants';

describe('helpCommand', () => {
  it('replies with help message', async () => {
    const ctx = { reply: vi.fn() } as any;
    await helpCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(helpBotMsg);
  });
});

