import PQueue from 'p-queue';

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
): Promise<T> {
  return queue.add(async () => {
    const headers = new Headers(options.headers);
    if (tokenProvider) {
      const token = await tokenProvider();
      if (token) headers.set('Authorization', `Bearer ${token}`);
    }
    if (impersonateUser) {
      headers.set('X-Impersonate-User', impersonateUser);
    }

    for (let attempt = 0; attempt < 3; attempt++) {
      const response = await fetch(`${baseUrl}${url}`, { ...options, headers });
      const data = await response.json().catch(() => undefined);

      if (response.ok) {
        return { data, status: response.status, headers: response.headers } as T;
      }

      if (response.status === 429) {
        const retryAfter = Number(response.headers.get('Retry-After') ?? '1') * 1000;
        await delay(retryAfter + Math.random() * 1000);
        continue;
      }

      if (attempt === 2) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const backoff = (2 ** attempt) * 100 + Math.random() * 100;
      await delay(backoff);
    }

    throw new Error('Request failed');
  });
}
