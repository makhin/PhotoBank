let tokenProvider: (() => string | undefined | Promise<string | undefined>) | undefined;
let baseUrl = '';
let impersonateUser: string | null = null;

export function configureApiAuth(provider: () => string | undefined | Promise<string | undefined>) {
  tokenProvider = provider;
}

export function configureApi(url: string) {
  baseUrl = url.replace(/\/$/, '');
}

export function setImpersonateUser(username: string | null | undefined) {
  impersonateUser = username ?? null;
}

export async function customFetcher<T>(url: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (tokenProvider) {
    const token = await tokenProvider();
    if (token) headers.set('Authorization', `Bearer ${token}`);
  }
  if (impersonateUser) {
    headers.set('X-Impersonate-User', impersonateUser);
  }

  const response = await fetch(`${baseUrl}${url}`, { ...options, headers });
  const data = await response.json().catch(() => undefined);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return { data, status: response.status, headers: response.headers } as T;
}
