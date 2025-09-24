import { describe, expect, it, vi, afterEach } from 'vitest';

import {
  clearSearchFilterTokens,
  registerSearchFilterToken,
  resolveSearchFilterToken,
} from '../src/cache/searchFilterCache';

const FIFTEEN_MINUTES = 15 * 60 * 1000;

describe('searchFilterCache', () => {
  afterEach(() => {
    clearSearchFilterTokens();
    vi.useRealTimers();
  });

  it('extends token expiration when the filter is resolved', () => {
    vi.useFakeTimers({ now: 0 });

    const filter = { caption: 'hello' };
    const token = registerSearchFilterToken(filter);

    vi.setSystemTime(FIFTEEN_MINUTES - 1000);
    expect(resolveSearchFilterToken(token)).toBe(filter);

    vi.setSystemTime(FIFTEEN_MINUTES + 100);
    expect(resolveSearchFilterToken(token)).toBe(filter);

    vi.setSystemTime(FIFTEEN_MINUTES * 2 + 200);
    expect(resolveSearchFilterToken(token)).toBeUndefined();
  });
});
