import { afterEach, describe, expect, it, vi } from 'vitest';

import { configureApiAuth, customFetcher } from '../src/api/photobank/fetcher';
import { ProblemDetailsError } from '../src/types/problem';
import type { TokenManager } from '../src/api/photobank/httpContext';

const originalFetch = globalThis.fetch;

afterEach(() => {
  configureApiAuth(undefined);
  if (originalFetch) {
    globalThis.fetch = originalFetch;
  } else {
    // @ts-expect-error -- Vitest adds fetch in the test environment, reset to undefined when absent
    delete globalThis.fetch;
  }
  vi.restoreAllMocks();
});

describe('customFetcher auth retry', () => {
  it('invokes auth error handler for ProblemDetails 401 responses before throwing', async () => {
    const getToken = vi
      .fn<TokenManager['getToken']>()
      .mockResolvedValueOnce('initial-token')
      .mockResolvedValueOnce('refreshed-token');

    const onAuthError = vi.fn();

    configureApiAuth({ getToken, onAuthError });

    const problem = {
      type: 'about:blank',
      title: 'Unauthorized',
      status: 401,
      detail: 'token expired',
    };

    const okResponse = { success: true };

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(
        new Response(JSON.stringify(problem), {
          status: 401,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(okResponse), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      );

    globalThis.fetch = fetchMock as unknown as typeof fetch;

    const result = await customFetcher<{ data: unknown; status: number }>('https://api.test/path', {
      method: 'GET',
      skipQueue: true,
    });

    expect(result.status).toBe(200);
    expect(result.data).toEqual(okResponse);

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(onAuthError).toHaveBeenCalledTimes(1);
    expect(onAuthError.mock.calls[0][1]).toBeInstanceOf(ProblemDetailsError);

    expect(getToken).toHaveBeenCalledTimes(2);
    expect(getToken.mock.calls[0][1]).toEqual({ forceRefresh: false });
    expect(getToken.mock.calls[1][1]).toEqual({ forceRefresh: true });

    const secondRequestInit = fetchMock.mock.calls[1]?.[1];
    expect(secondRequestInit).toBeDefined();
    const headers = new Headers(secondRequestInit!.headers);
    expect(headers.get('Authorization')).toBe('Bearer refreshed-token');
  });
});
