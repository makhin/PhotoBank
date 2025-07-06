import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('filterResultsCache', () => {
  beforeEach(() => {
    vi.resetModules();
    // ensure non-browser environment
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    delete (global as any).window;
  });

  it('stores and retrieves photos by hash', async () => {
    const { cacheFilterResult, getCachedFilterResult } = await import('../src/cache/filterResultsCache');
    const photos = [{ id: 1 }, { id: 2 }];
    await cacheFilterResult('abc', { count: 2, photos });
    const cached = await getCachedFilterResult('abc');
    expect(cached?.count).toBe(2);
    expect(cached?.photos).toEqual(photos);
  });
});
