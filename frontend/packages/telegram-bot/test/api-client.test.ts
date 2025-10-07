import type { Context } from 'grammy';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const state: {
    baseUrl: string | undefined;
    authConfig:
      | {
          getToken?: (context: unknown, options?: { forceRefresh?: boolean }) => unknown;
          onAuthError?: (context: unknown, error?: unknown) => unknown;
        }
      | undefined;
  } = {
    baseUrl: undefined,
    authConfig: undefined,
  };

  return {
    get baseUrl() {
      return state.baseUrl;
    },
    get authConfig() {
      return state.authConfig;
    },
    configureApi: vi.fn((url?: string) => {
      state.baseUrl = url;
    }),
    applyHttpContext: vi.fn((config) => {
      if (!config) {
        state.authConfig = undefined;
        return;
      }

      if ('auth' in config) {
        const auth = config.auth;
        if (!auth) {
          state.authConfig = undefined;
        } else if (typeof auth === 'function') {
          state.authConfig = { getToken: auth };
        } else {
          state.authConfig = auth;
        }
      }
    }),
    getRequestContext: vi.fn(),
    runWithRequestContext: vi.fn(),
    ensureUserAccessToken: vi.fn<[
      Context,
      boolean | undefined,
    ], Promise<string>>(),
    invalidateUserToken: vi.fn<[Context], void>(),
  };
});

vi.mock('@photobank/shared/api/photobank', () => ({
  configureApi: mocks.configureApi,
  getRequestContext: mocks.getRequestContext,
  runWithRequestContext: mocks.runWithRequestContext,
}));

vi.mock('@photobank/shared/api/photobank/httpContext', () => ({
  applyHttpContext: mocks.applyHttpContext,
}));

vi.mock('@/auth', () => ({
  ensureUserAccessToken: mocks.ensureUserAccessToken,
  invalidateUserToken: mocks.invalidateUserToken,
}));

beforeAll(async () => {
  process.env.API_BASE_URL = 'https://api.test';
  await import('../src/api/client');
});

const {
  configureApi,
  getRequestContext,
  ensureUserAccessToken,
  invalidateUserToken,
} = mocks;

describe('api/client', () => {
  const ctx = { from: { id: 42 } } as unknown as Context;

  beforeEach(() => {
    getRequestContext.mockReset();
    ensureUserAccessToken.mockReset();
    invalidateUserToken.mockReset();
  });

  it('configures base URL once on import', () => {
    expect(mocks.baseUrl).toBe('https://api.test');
  });

  it('resolves tokens using current context and supports forced refresh', async () => {
    const authConfig = mocks.authConfig;
    if (!authConfig) throw new Error('applyHttpContext not called');

    ensureUserAccessToken.mockResolvedValue('token');
    const token = await authConfig.getToken?.(ctx, { forceRefresh: true });

    expect(ensureUserAccessToken).toHaveBeenCalledWith(ctx, true);
    expect(token).toBe('token');
  });

  it('falls back to stored context when none provided', async () => {
    const authConfig = mocks.authConfig;
    if (!authConfig) throw new Error('applyHttpContext not called');

    getRequestContext.mockReturnValue(ctx);
    ensureUserAccessToken.mockResolvedValue('token2');

    const token = await authConfig.getToken?.(undefined, {});

    expect(getRequestContext).toHaveBeenCalled();
    expect(ensureUserAccessToken).toHaveBeenCalledWith(ctx, false);
    expect(token).toBe('token2');
  });

  it('invalidates tokens on auth failures', async () => {
    const authConfig = mocks.authConfig;
    if (!authConfig) throw new Error('applyHttpContext not called');

    getRequestContext.mockReturnValue(ctx);

    await authConfig.onAuthError?.(undefined);

    expect(invalidateUserToken).toHaveBeenCalledWith(ctx);
  });
});
