import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProblemDetailsError } from '@photobank/shared/types/problem';

import type { MyContext } from '../src/i18n';
import { ensureRegistered } from '../src/registration';

const ensureUserAccessToken = vi.hoisted(() =>
  vi.fn<[unknown, boolean | undefined], Promise<string>>()
);

vi.mock('../src/auth', () => ({
  ensureUserAccessToken,
}));

describe('ensureRegistered', () => {
  beforeEach(() => {
    ensureUserAccessToken.mockReset();
  });

  it('replies with not registered message when backend returns ProblemDetails 403', async () => {
    const reply = vi.fn<[string], Promise<void>>(() => Promise.resolve());
    const t = vi.fn<[string, Record<string, unknown> | undefined], string>((key, params) => {
      if (key === 'not-registered') {
        return `not-registered:${params?.userId ?? 'missing'}`;
      }
      return key;
    });

    const ctx = {
      from: { id: 123 },
      reply,
      t,
    } as unknown as MyContext;

    ensureUserAccessToken.mockRejectedValue(
      new ProblemDetailsError({ title: 'Forbidden', status: 403, detail: 'no access' })
    );

    const result = await ensureRegistered(ctx);

    expect(result).toBe(false);
    expect(ensureUserAccessToken).toHaveBeenCalledWith(ctx);
    expect(t).toHaveBeenCalledWith('not-registered', { userId: 123 });
    expect(reply).toHaveBeenCalledWith('not-registered:123');
  });
});
