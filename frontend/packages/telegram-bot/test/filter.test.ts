import { describe, it, expect, vi } from 'vitest';
import { filterCommand, handleFilterWizard } from '../src/commands/filter';

describe('filterCommand wizard', () => {
  it('starts wizard and asks for storage', async () => {
    const ctx = { chat: { id: 1 }, reply: vi.fn() } as any;
    await filterCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith('Введите ID хранилища:');
  });

  it('handles first step and asks for date', async () => {
    const ctxStart = { chat: { id: 2 }, reply: vi.fn() } as any;
    await filterCommand(ctxStart);
    const ctx = { chat: { id: 2 }, message: { text: '1' }, reply: vi.fn() } as any;
    await handleFilterWizard(ctx);
    expect(ctx.reply).toHaveBeenCalledWith('Введите дату YYYY-MM-DD:');
  });
});
