import { describe, it, expect, vi } from 'vitest';
import { subscribeCommand, parseSubscribeTime, subscriptions } from '../src/commands/subscribe';

vi.mock('../src/services/auth', () => ({
  updateUser: vi.fn().mockResolvedValue(undefined),
}));
import { i18n } from '../src/i18n';

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
    const ctx = { reply: vi.fn(), message: { text: '/subscribe' }, chat: { id: 1 }, t: (k: string) => i18n.t('en', k), i18n: { locale: () => 'en' } } as any;
    await subscribeCommand(ctx);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'subscribe-usage'));
  });

  it('stores subscription on valid input', async () => {
    const ctx = { reply: vi.fn(), message: { text: '/subscribe 07:15' }, chat: { id: 42 }, t: (k: string, p?: any) => i18n.t('en', k, p), i18n: { locale: () => 'en' } } as any;
    await subscribeCommand(ctx);
    expect(subscriptions.get(42)?.time).toBe('07:15');
  });
});
