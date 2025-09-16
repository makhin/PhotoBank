import type { Context } from 'grammy';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HttpError } from '@photobank/shared/types/problem';

const mocks = vi.hoisted(() => {
  const request = vi.fn();
  return {
    ensureUserAccessToken: vi.fn<
      [Context, boolean | undefined],
      Promise<string>
    >(),
    invalidateUserToken: vi.fn<
      [Context | { from?: { id?: number } }],
      void
    >(),
    request,
    create: vi.fn(() => ({ request })),
    isAxiosError: vi.fn(() => false),
  };
});

vi.mock('@/auth', () => ({
  ensureUserAccessToken: mocks.ensureUserAccessToken,
  invalidateUserToken: mocks.invalidateUserToken,
}));

vi.mock('axios', () => ({
  default: {
    create: mocks.create,
    isAxiosError: mocks.isAxiosError,
  },
  create: mocks.create,
  isAxiosError: mocks.isAxiosError,
}));

const { ensureUserAccessToken, invalidateUserToken, request } = mocks;

import { photobankAxios } from '../src/api/axios-instance';

describe('photobankAxios', () => {
  beforeEach(() => {
    ensureUserAccessToken.mockReset();
    invalidateUserToken.mockReset();
    request.mockReset();
  });

  it('retries when ensureUserAccessToken throws HttpError 403', async () => {
    const ctx = { from: { id: 123 } } as unknown as Context;
    const responseData = { ok: true };

    request.mockResolvedValue({ data: responseData });
    ensureUserAccessToken.mockImplementation(async (_ctx, force) => {
      if (!force) {
        throw new HttpError(403);
      }
      return 'fresh-token';
    });

    const result = await photobankAxios<typeof responseData>({ url: '/photos' }, ctx);

    expect(result).toEqual(responseData);
    expect(ensureUserAccessToken).toHaveBeenCalledTimes(2);
    expect(ensureUserAccessToken).toHaveBeenNthCalledWith(1, ctx, false);
    expect(ensureUserAccessToken).toHaveBeenNthCalledWith(2, ctx, true);
    expect(invalidateUserToken).toHaveBeenCalledWith(ctx);
    expect(request).toHaveBeenCalledTimes(1);
    expect(request).toHaveBeenCalledWith({
      url: '/photos',
      headers: { Authorization: 'Bearer fresh-token' },
    });
  });
});
