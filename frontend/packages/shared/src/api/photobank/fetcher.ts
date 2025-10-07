import PQueue from 'p-queue';

import { HttpError, ProblemDetailsError, isProblemDetails } from '../../types/problem';
import {
  getImpersonateUser,
  getRetryPolicy,
  getTokenManager,
  notifyAuthError,
  resolveAuthToken,
  setImpersonateUser as updateImpersonateUser,
  setTokenManager,
  type MaybeTokenManager,
} from './httpContext';

type CustomRequestInit = RequestInit & { skipQueue?: boolean };

let baseUrl = '';
let currentContext: unknown | undefined;

export function configureApiAuth(manager?: MaybeTokenManager) {
  setTokenManager(manager);
}

export function configureApi(url: string) {
  baseUrl = (url ?? '').replace(/\/$/, '');
}

export function setImpersonateUser(username: string | null | undefined) {
  updateImpersonateUser(username);
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

export async function customFetcher<T>(url: string, init?: CustomRequestInit): Promise<T> {
  const { skipQueue, ...requestInit } = init ?? {};

  const execute = async (): Promise<T> => {
    const policy = getRetryPolicy();
    const totalAttempts = Math.max(1, policy.attempts);

    for (let attempt = 0; attempt < totalAttempts; attempt++) {
      let token: string | undefined;
      try {
        token = await resolveAuthToken(currentContext, { forceRefresh: attempt > 0 });
      } catch (error) {
        const http = asHttpError(error);
        const shouldRetry =
          !!getTokenManager() &&
          http?.status != null &&
          [401, 403].includes(http.status) &&
          attempt < totalAttempts - 1;
        if (shouldRetry) {
          await notifyAuthError(currentContext, error);
          continue;
        }
        throw error;
      }

      const normalized = normalizeInit(requestInit as RequestInit);
      const headers = new Headers(normalized.headers);

      if (token && !headers.has('Authorization')) {
        headers.set('Authorization', `Bearer ${token}`);
      }

      const impersonateUser = getImpersonateUser();
      if (impersonateUser) {
        headers.set('X-Impersonate-User', impersonateUser);
      }

      const finalInit: RequestInit = { ...normalized, headers };

      try {
        const requestUrl = buildUrl(url);
        const response = await fetch(requestUrl, finalInit);
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

        const problemDetailsError = isProblemDetails(errorData)
          ? new ProblemDetailsError(errorData)
          : undefined;

        const httpError = new HttpError(response.status, {
          url: requestUrl,
          method: finalInit.method,
          body: errorData,
        });

        if (getTokenManager() && [401, 403].includes(response.status) && attempt < totalAttempts - 1) {
          await notifyAuthError(currentContext, problemDetailsError ?? httpError);
          continue;
        }

        if (problemDetailsError) {
          throw problemDetailsError;
        }

        if (policy.shouldRetry({ attempt, response }) && attempt < totalAttempts - 1) {
          const backoff = policy.getDelayMs({ attempt, response });
          if (backoff > 0) {
            await delay(backoff);
          }
          continue;
        }

        throw httpError;
      } catch (error) {
        if (isAbortError(error)) throw error;

        if (error instanceof ProblemDetailsError) throw error;

        const http = asHttpError(error);
        const shouldRetry = policy.shouldRetry({ attempt, error });

        if (shouldRetry && attempt < totalAttempts - 1) {
          const backoff = policy.getDelayMs({ attempt, error });
          if (backoff > 0) {
            await delay(backoff);
          }
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