import { randomBytes } from 'node:crypto';
import type { FilterDto } from '@photobank/shared/api/photobank';

const TOKEN_TTL_MS = 1000 * 60 * 15; // 15 minutes
const MAX_ENTRIES = 1000;

interface CacheEntry {
  filter: FilterDto;
  expiresAt: number;
}

const cache = new Map<string, CacheEntry>();

function generateToken(): string {
  return randomBytes(9).toString('base64url');
}

function purgeExpired(now = Date.now()) {
  for (const [token, entry] of cache) {
    if (entry.expiresAt <= now) {
      cache.delete(token);
    }
  }
}

function ensureCapacity() {
  if (cache.size <= MAX_ENTRIES) return;

  const entries = [...cache.entries()].sort(
    (a, b) => a[1].expiresAt - b[1].expiresAt,
  );

  while (cache.size > MAX_ENTRIES && entries.length) {
    const [token] = entries.shift()!;
    cache.delete(token);
  }
}

export function registerSearchFilterToken(filter: FilterDto): string {
  const now = Date.now();
  purgeExpired(now);

  let token: string;
  do {
    token = generateToken();
  } while (cache.has(token));

  cache.set(token, {
    filter,
    expiresAt: now + TOKEN_TTL_MS,
  });

  ensureCapacity();

  return token;
}

export function resolveSearchFilterToken(token: string): FilterDto | undefined {
  const now = Date.now();
  purgeExpired(now);

  const entry = cache.get(token);
  if (!entry) {
    return undefined;
  }
  if (entry.expiresAt <= now) {
    cache.delete(token);
    return undefined;
  }

  return entry.filter;
}

export function clearSearchFilterTokens() {
  cache.clear();
}
