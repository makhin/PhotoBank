import { describe, it, expect, vi } from 'vitest';
import { helpCommand } from '../src/commands/help';
import { i18n } from '../src/i18n';

describe('helpCommand', () => {
  it('replies with localized help message', async () => {
    const ctx = { reply: vi.fn(), t: (key: string) => i18n.t('en', key) } as any;
    await helpCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'help'));
  });
});

