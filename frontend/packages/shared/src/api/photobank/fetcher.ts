import PQueue from 'p-queue';
import { ProblemDetailsError, HttpError, isProblemDetails } from '../../types/problem';

let tokenProvider: (() => string | undefined | Promise<string | undefined>) | undefined;
let baseUrl = '';
let impersonateUser: string | null = null;

const queue = new PQueue({ interval: 1000, intervalCap: 5 });

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function configureApiAuth(provider: () => string | undefined | Promise<string | undefined>) {
  tokenProvider = provider;
}

export function configureApi(url: string) {
  baseUrl = url.replace(/\/$/, '');
}

export function setImpersonateUser(username: string | null | undefined) {
  impersonateUser = username ?? null;
}

export async function customFetcher<T>(
  url: string,
  options: RequestInit = {},
  signal?: AbortSignal,
): Promise<T> {
  return queue.add(async (): Promise<T> => {
    const headers = new Headers(options.headers);
    if (tokenProvider) {
      const token = await tokenProvider();
      if (token) headers.set('Authorization', `Bearer ${token}`);
    }
    if (impersonateUser) {
      headers.set('X-Impersonate-User', impersonateUser);
    }

    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        const response = await fetch(`${baseUrl}${url}`, { ...options, headers, signal });
        const data = await response.json().catch(() => undefined);

        if (response.ok) {
          return { data, status: response.status, headers: response.headers } as T;
        }

        if (isProblemDetails(data)) {
          throw new ProblemDetailsError(data);
        }

        const err = new HttpError(response.status, {
          url: `${baseUrl}${url}`,
          method: options.method,
          body: data,
        });

        if (response.status >= 500 && attempt < 2) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }
        throw err;
      } catch (e: any) {
        if (e.name === 'AbortError') throw e;
        if (attempt < 2 && (e instanceof TypeError || e instanceof HttpError && e.status >= 500)) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }
        throw e;
      }
    }
    throw new Error('Request failed');
  }) as Promise<T>;
}
