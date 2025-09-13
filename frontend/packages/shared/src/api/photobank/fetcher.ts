import PQueue from 'p-queue';
import { ProblemDetailsError, HttpError, isProblemDetails } from '../../types/problem';

// ====== конфиг ======
let tokenProvider: (() => string | undefined | Promise<string | undefined>) | undefined;
let baseUrl = '';
let impersonateUser: string | null = null;

export function configureApiAuth(provider: () => string | undefined | Promise<string | undefined>) {
  tokenProvider = provider;
}
export function configureApi(url: string) {
  baseUrl = (url ?? '').replace(/\/$/, '');
}
export function setImpersonateUser(username: string | null | undefined) {
  impersonateUser = username ?? null;
}

// ====== утилиты ======
const queue = new PQueue({ interval: 1000, intervalCap: 5 });

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
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
  const txt = await res.text().catch(() => '');
  return (txt as unknown) as T;
}

// ====== мутатор для orval ======
export async function customFetcher<T>(url: string, init?: RequestInit): Promise<T> {
  return queue.add<T>(async (): Promise<T> => {
    const normalized = normalizeInit(init);
    const headers = new Headers(normalized.headers);

    // auth / impersonation
    if (tokenProvider) {
      const token = await tokenProvider();
      if (token && !headers.has('Authorization')) headers.set('Authorization', `Bearer ${token}`);
    }
    if (impersonateUser) headers.set('X-Impersonate-User', impersonateUser);

    const finalInit: RequestInit = { ...normalized, headers };

    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        const response = await fetch(buildUrl(url), finalInit);
        if (response.ok) {
          const data = await parseBody<unknown>(response);
          return { data, status: response.status, headers: response.headers } as T;
        }

        const errorText = await response.text().catch(() => '');
        let errorData: unknown = undefined;
        try {
          errorData = errorText ? JSON.parse(errorText) : undefined;
        } catch {
          errorData = errorText || undefined;
        }

        if (isProblemDetails(errorData)) throw new ProblemDetailsError(errorData);

        const err = new HttpError(response.status, {
          url: buildUrl(url),
          method: finalInit.method,
          body: errorData,
        });

        if (response.status >= 500 && attempt < 2) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }
        throw err;
      } catch (e: unknown) {
        const err = e as { name?: string; code?: string; message?: string };
        const isAbort =
          err?.name === 'AbortError' ||
          err?.code === 'ABORT_ERR' ||
          (typeof err?.message === 'string' && /aborted/i.test(err.message));
        if (isAbort) throw e;

        const isNetwork = e instanceof TypeError;
        const is5xx = e instanceof HttpError && e.status >= 500;
        if ((isNetwork || is5xx) && attempt < 2) {
          const backoff = 2 ** attempt * 100 + Math.random() * 100;
          await delay(backoff);
          continue;
        }
        throw e;
      }
    }

    throw new Error('Request failed after retries');
  }) as Promise<T>;
}

export { customFetcher as fetcher };