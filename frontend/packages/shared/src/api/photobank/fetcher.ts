import PQueue from 'p-queue';

import { HttpError, ProblemDetailsError, isProblemDetails } from '../../types/problem';

type TokenProvider = (
  context: unknown | undefined,
  options?: { forceRefresh?: boolean },
) => string | undefined | Promise<string | undefined>;

type TokenManager = {
  getToken: TokenProvider;
  onAuthError?: (context: unknown | undefined, error: unknown) => void | Promise<void>;
};

type MaybeTokenManager = TokenManager | TokenProvider | undefined;

type CustomRequestInit = RequestInit & { skipQueue?: boolean };

let tokenManager: TokenManager | undefined;
let baseUrl = '';
let impersonateUser: string | null = null;
let currentContext: unknown | undefined;

export function configureApiAuth(manager?: MaybeTokenManager) {
  if (!manager) {
    tokenManager = undefined;
    return;
  }

  tokenManager = typeof manager === 'function' ? { getToken: manager } : manager;
}

export function configureApi(url: string) {
  baseUrl = (url ?? '').replace(/\/$/, '');
}

export function setImpersonateUser(username: string | null | undefined) {
  impersonateUser = username ?? null;
}

export function runWithRequestContext<T>(context: unknown, fn: () => Promise<T> | T): Promise<T> {
  const previous = currentContext;
  currentContext = context;

  let result: Promise<T> | T;
  try {
    result = fn();
  } catch (error) {
    currentContext = previous;
    throw error;
  }

  if (result instanceof Promise) {
    return result.finally(() => {
      currentContext = previous;
    });
  }

  currentContext = previous;
  return Promise.resolve(result);
}

export function getRequestContext<T = unknown>() {
  return currentContext as T | undefined;
}

const queue = new PQueue({ interval: 1000, intervalCap: 5 });

function delay(ms: number) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

function buildUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) return url;
  if (!baseUrl) return url;
  const left = baseUrl.replace(/\/$/, '');
  const right = url.startsWith('/') ? url : `/${url}`;
  return `${left}${right}`;
}

function normalizeInit(init?: RequestInit): RequestInit {
  const headers = new Headers(init?.headers ?? {});
  if (!headers.has('Accept')) headers.set('Accept', 'application/json');

  const bodyIsJson =
    init?.body != null &&
    !(init.body instanceof FormData) &&
    !(init.body instanceof Blob) &&
    typeof init.body !== 'string';

  if (bodyIsJson && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  return {
    ...init,
    headers,
    body: bodyIsJson ? JSON.stringify(init!.body) : init?.body ?? null,
    credentials: init?.credentials ?? 'include',
  };
}

async function parseBody<T>(res: Response): Promise<T> {
  if (res.status === 204) return undefined as unknown as T;
  const ct = res.headers.get('content-type') ?? '';
  if (ct.includes('application/json')) return (await res.json()) as T;
  if (ct.includes('application/octet-stream')) return (await res.arrayBuffer()) as unknown as T;
  const txt = await res.text().catch(() => '');
  return (txt as unknown) as T;
}

function isAbortError(error: unknown) {
  const err = error as { name?: string; code?: string; message?: string };
  return (
    err?.name === 'AbortError' ||
    err?.code === 'ABORT_ERR' ||
    (typeof err?.message === 'string' && /aborted/i.test(err.message))
  );
}

function asHttpError(error: unknown): HttpError | undefined {
  return error instanceof HttpError ? error : undefined;
}

async function resolveToken(forceRefresh: boolean) {
  if (!tokenManager) return undefined;
  return tokenManager.getToken(currentContext, { forceRefresh });
}

export async function customFetcher<T>(url: string, init?: CustomRequestInit): Promise<T> {
  const { skipQueue, ...requestInit } = init ?? {};

  const execute = async (): Promise<T> => {
    for (let attempt = 0; attempt < 3; attempt++) {
      let token: string | undefined;
      try {
        token = await resolveToken(attempt > 0);
      } catch (error) {
        const http = asHttpError(error);
        const shouldRetry =
          !!tokenManager && http?.status != null && [401, 403].includes(http.status) && attempt < 2;
        if (shouldRetry) {
          await tokenManager!.onAuthError?.(currentContext, error);
          continue;
        }
        throw error;
      }

      const normalized = normalizeInit(requestInit as RequestInit);
      const headers = new Headers(normalized.headers);

      if (token && !headers.has('Authorization')) {
        headers.set('Authorization', `Bearer ${token}`);
      }

      if (impersonateUser) {
        headers.set('X-Impersonate-User', impersonateUser);
      }

      const finalInit: RequestInit = { ...normalized, headers };

      try {
        const response = await fetch(buildUrl(url), finalInit);
        if (response.ok) {
          const data = await parseBody<unknown>(response);
          return { data, status: response.status, headers: response.headers } as T;
        }

        const rawText = await response.text().catch(() => '');
        let errorData: unknown = undefined;
        try {
          errorData = rawText ? JSON.parse(rawText) : undefined;
        } catch {
          errorData = rawText || undefined;
        }

        if (isProblemDetails(errorData)) {
          throw new ProblemDetailsError(errorData);
        }

        const httpError = new HttpError(response.status, {
          url: buildUrl(url),
          method: finalInit.method,
          body: errorData,
        });

        if (tokenManager && [401, 403].includes(response.status) && attempt < 2) {
          await tokenManager.onAuthError?.(currentContext, httpError);
          continue;
        }

        if (response.status >= 500 && attempt < 2) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }

        throw httpError;
      } catch (error) {
        if (isAbortError(error)) throw error;

        if (error instanceof ProblemDetailsError) throw error;

        const http = asHttpError(error);
        const shouldRetry =
          (http?.status != null && http.status >= 500) || error instanceof TypeError;

        if (shouldRetry && attempt < 2) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }

        throw error;
      }
    }

    throw new Error('Request failed after retries');
  };

  return skipQueue ? execute() : (queue.add(execute) as Promise<T>);
}

export { customFetcher as fetcher };