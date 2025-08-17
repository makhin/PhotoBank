import { describe, it, expect, vi } from 'vitest';
import { subscribeCommand, parseSubscribeTime, subscriptions } from '../src/commands/subscribe';

vi.mock('../src/services/auth', () => ({
  updateUser: vi.fn().mockResolvedValue(undefined),
}));
import { subscribeCommandUsageMsg } from '@photobank/shared/constants';

describe('parseSubscribeTime', () => {
  it('parses valid time', () => {
    expect(parseSubscribeTime('/subscribe 08:30')).toBe('08:30');
  });

  it('returns null for invalid time', () => {
    expect(parseSubscribeTime('/subscribe 25:00')).toBeNull();
  });
});

describe('subscribeCommand', () => {
  it('replies with usage on wrong input', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/subscribe' }, chat: { id: 1 } } as any;
    await subscribeCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(subscribeCommandUsageMsg);
  });

  it('stores subscription on valid input', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/subscribe 07:15' }, chat: { id: 42 } } as any;
    await subscribeCommand(ctx);
    expect(subscriptions.get(42)).toBe('07:15');
  });
});
