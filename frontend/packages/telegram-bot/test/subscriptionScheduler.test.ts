import { describe, expect, it, vi } from 'vitest';
import type { Bot } from 'grammy';

import type { MyContext } from '../src/i18n';

const sendThisDayPage = vi.hoisted(() =>
  vi.fn<[MyContext, number, boolean?], Promise<void>>(() => Promise.resolve()),
);

vi.mock('../src/commands/thisday', () => ({
  sendThisDayPage,
}));

import { initSubscriptionScheduler, subscriptions } from '../src/commands/subscribe';

describe('initSubscriptionScheduler', () => {
  it('reuses stored telegram user info when triggering scheduled send', () => {
    subscriptions.clear();
    sendThisDayPage.mockClear();
    vi.useFakeTimers();
    const intervalSpy = vi.spyOn(globalThis, 'setInterval');

    try {
      const bot = { api: { sendMessage: vi.fn(() => Promise.resolve(undefined)) } } as unknown as Bot<MyContext>;
      initSubscriptionScheduler(bot);

      expect(intervalSpy).toHaveBeenCalledTimes(1);
      const intervalCallback = intervalSpy.mock.calls[0]![0] as () => void;

      const from: NonNullable<MyContext['from']> = {
        id: 111,
        is_bot: false,
        first_name: 'Scheduler',
        username: 'scheduler_user',
      };

      vi.setSystemTime(new Date('2024-01-01T05:10:00Z'));
      subscriptions.set(123, {
        time: '05:10',
        locale: 'en',
        from,
      });

      expect(() => intervalCallback()).not.toThrow();

      expect(sendThisDayPage).toHaveBeenCalledWith(
        expect.objectContaining({
          from: expect.objectContaining({
            id: from.id,
            username: from.username,
          }),
        }),
        1,
      );
    } finally {
      subscriptions.clear();
      intervalSpy.mockRestore();
      vi.useRealTimers();
    }
  });
});

