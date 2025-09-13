import { describe, it, expect, vi } from 'vitest';
import { ensureUserAccessToken, invalidateUserToken } from '../src/auth';
import * as apiAuth from '../src/api/auth';

describe('ensureUserAccessToken', () => {
  it('respects cache, force flag and invalidation', async () => {
    const ctx = { from: { id: 1, username: 'user' } } as any;
    const spy = vi
      .spyOn(apiAuth, 'exchangeTelegramUserToken')
      .mockResolvedValueOnce({ accessToken: 't1', expiresIn: 3600 })
      .mockResolvedValueOnce({ accessToken: 't2', expiresIn: 3600 })
      .mockResolvedValueOnce({ accessToken: 't3', expiresIn: 3600 });

    const token1 = await ensureUserAccessToken(ctx);
    const token2 = await ensureUserAccessToken(ctx);
    const token3 = await ensureUserAccessToken(ctx, true);
    const token4 = await ensureUserAccessToken(ctx);
    invalidateUserToken(ctx);
    const token5 = await ensureUserAccessToken(ctx);

    expect(token1).toBe('t1');
    expect(token2).toBe('t1');
    expect(token3).toBe('t2');
    expect(token4).toBe('t2');
    expect(token5).toBe('t3');
    expect(spy).toHaveBeenCalledTimes(3);
  });
});
