import type { HttpError } from '../../types/problem';

export type TokenProvider = (
  context: unknown | undefined,
  options?: { forceRefresh?: boolean },
) => string | undefined | Promise<string | undefined>;

export type TokenManager = {
  getToken: TokenProvider;
  onAuthError?: (context: unknown | undefined, error: unknown) => void | Promise<void>;
};

export type MaybeTokenManager = TokenManager | TokenProvider | undefined;

export interface RetryAttemptContext {
  attempt: number;
  error?: unknown;
  response?: Response;
}

export interface HttpRetryPolicy {
  attempts: number;
  shouldRetry: (context: RetryAttemptContext) => boolean;
  getDelayMs: (context: RetryAttemptContext) => number;
}

export interface HttpContextConfig {
  auth?: MaybeTokenManager | null;
  impersonateUser?: string | null;
  retry?: Partial<HttpRetryPolicy> | null;
}

const DEFAULT_RETRY_POLICY: HttpRetryPolicy = {
  attempts: 3,
  shouldRetry: ({ response, error }) => {
    if (response) return response.status >= 500;

    if (error instanceof TypeError) return true;

    const status = (error as HttpError | { status?: number } | undefined)?.status;
    if (typeof status === 'number') {
      return status >= 500;
    }

    return false;
  },
  getDelayMs: ({ attempt }) => 2 ** attempt * 100 + Math.random() * 100,
};

let tokenManager: TokenManager | undefined;
let impersonatedUser: string | null = null;
let retryPolicy: HttpRetryPolicy = { ...DEFAULT_RETRY_POLICY };

function normalizeManager(manager: MaybeTokenManager): TokenManager | undefined {
  if (!manager) return undefined;
  return typeof manager === 'function' ? { getToken: manager } : manager;
}

export function setTokenManager(manager?: MaybeTokenManager) {
  tokenManager = normalizeManager(manager);
}

export function getTokenManager() {
  return tokenManager;
}

export async function resolveAuthToken(
  context: unknown | undefined,
  options: { forceRefresh: boolean },
) {
  if (!tokenManager) return undefined;
  return tokenManager.getToken(context, { forceRefresh: options.forceRefresh });
}

export async function notifyAuthError(context: unknown | undefined, error: unknown) {
  await tokenManager?.onAuthError?.(context, error);
}

export function setImpersonateUser(username: string | null | undefined) {
  impersonatedUser = username ?? null;
}

export function getImpersonateUser() {
  return impersonatedUser;
}

export function resetRetryPolicy() {
  retryPolicy = { ...DEFAULT_RETRY_POLICY };
}

export function setRetryPolicy(policy?: Partial<HttpRetryPolicy> | null) {
  if (!policy) {
    resetRetryPolicy();
    return;
  }

  retryPolicy = {
    attempts: Math.max(1, policy.attempts ?? retryPolicy.attempts),
    shouldRetry: policy.shouldRetry ?? retryPolicy.shouldRetry,
    getDelayMs: policy.getDelayMs ?? retryPolicy.getDelayMs,
  };
}

export function getRetryPolicy(): HttpRetryPolicy {
  return retryPolicy;
}

export function applyHttpContext(config: HttpContextConfig) {
  if ('auth' in config) {
    setTokenManager(config.auth ?? undefined);
  }

  if ('impersonateUser' in config) {
    setImpersonateUser(config.impersonateUser ?? null);
  }

  if ('retry' in config) {
    setRetryPolicy(config.retry ?? undefined);
  }
}

export function getDefaultRetryPolicy() {
  return { ...DEFAULT_RETRY_POLICY };
}
