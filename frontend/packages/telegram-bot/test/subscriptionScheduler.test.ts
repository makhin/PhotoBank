import { describe, expect, it, vi } from 'vitest';
import type { Bot } from 'grammy';

import type { MyContext } from '../src/i18n';

const sendThisDayPage = vi.hoisted(() =>
  vi.fn<[MyContext, number, boolean?], Promise<void>>(() => Promise.resolve()),
);

const fetchTelegramSubscriptions = vi.hoisted(() =>
  vi.fn<[], Promise<Array<{ telegramUserId: number; telegramSendTimeUtc: string }>>>(
    () => Promise.resolve([]),
  ),
);

vi.mock('../src/commands/thisday', () => ({
  sendThisDayPage,
}));

vi.mock('../src/api/auth', async () => {
  const actual = await vi.importActual<typeof import('../src/api/auth')>('../src/api/auth');
  return {
    ...actual,
    fetchTelegramSubscriptions,
  };
});

import { initSubscriptionScheduler, restoreSubscriptions, subscriptions } from '../src/commands/subscribe';

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

  it('restores saved subscriptions before scheduler tick', async () => {
    subscriptions.clear();
    sendThisDayPage.mockClear();
    fetchTelegramSubscriptions.mockClear();
    fetchTelegramSubscriptions.mockResolvedValueOnce([
      { telegramUserId: 123, telegramSendTimeUtc: '05:10:00' },
    ]);

    const getChat = vi.fn(() =>
      Promise.resolve({
        id: 123,
        type: 'private',
        first_name: 'Restored',
        username: 'restored_user',
      }),
    );
    const bot = {
      api: {
        sendMessage: vi.fn(() => Promise.resolve(undefined)),
        getChat,
      },
    } as unknown as Bot<MyContext>;

    await restoreSubscriptions(bot);

    expect(fetchTelegramSubscriptions).toHaveBeenCalledTimes(1);
    expect(getChat).toHaveBeenCalledWith(123);
    expect(subscriptions.get(123)).toMatchObject({
      time: '05:10',
      locale: 'en',
      from: expect.objectContaining({
        id: 123,
        first_name: 'Restored',
        username: 'restored_user',
      }),
    });

    vi.useFakeTimers();
    const intervalSpy = vi.spyOn(globalThis, 'setInterval');

    try {
      initSubscriptionScheduler(bot);

      expect(intervalSpy).toHaveBeenCalledTimes(1);
      const intervalCallback = intervalSpy.mock.calls[0]![0] as () => void;

      vi.setSystemTime(new Date('2024-01-01T05:10:00Z'));
      intervalCallback();

      expect(sendThisDayPage).toHaveBeenCalledWith(
        expect.objectContaining({
          chat: expect.objectContaining({ id: 123 }),
          from: expect.objectContaining({ id: 123 }),
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

