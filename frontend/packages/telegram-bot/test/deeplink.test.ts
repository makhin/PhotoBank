import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { i18n } from '../src/i18n';
import { ProblemDetailsError } from '@photobank/shared/types/problem';
import * as auth from '../src/auth';

let handler: (ctx: any, next: () => Promise<void>) => Promise<void>;

beforeAll(async () => {
  const { bot } = await import('../src/bot');
  const onSpy = vi.spyOn(bot, 'on');
  await import('../src/handlers/deeplink');
  handler = onSpy.mock.calls[0][1];
  onSpy.mockRestore();
});

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('deeplink handler', () => {
  it('links user on base /start', async () => {
    vi.spyOn(auth, 'ensureUserAccessToken').mockResolvedValue('token');
    const ctx = {
      message: { text: '/start' },
      from: { id: 42 },
      reply: vi.fn(),
      t: (k: string, vars?: any) => i18n.t('en', k, vars),
    } as any;
    await handler(ctx, async () => {});
    expect(auth.ensureUserAccessToken).toHaveBeenCalled();
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'start-linked'));
  });

  it('replies not-registered on forbidden error', async () => {
    vi.spyOn(auth, 'ensureUserAccessToken').mockRejectedValue(
      new ProblemDetailsError({ title: 'Forbidden', status: 403 })
    );
    const ctx = {
      message: { text: '/start' },
      from: { id: 42 },
      reply: vi.fn(),
      t: (k: string, vars?: any) => i18n.t('en', k, vars),
    } as any;
    await handler(ctx, async () => {});
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'not-registered', { userId: 42 }));
  });

  it('replies sorry-try-later on other errors', async () => {
    vi.spyOn(auth, 'ensureUserAccessToken').mockRejectedValue(new Error('fail'));
    const ctx = {
      message: { text: '/start' },
      from: { id: 42 },
      reply: vi.fn(),
      t: (k: string, vars?: any) => i18n.t('en', k, vars),
    } as any;
    await handler(ctx, async () => {});
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'sorry-try-later'));
  });
});

