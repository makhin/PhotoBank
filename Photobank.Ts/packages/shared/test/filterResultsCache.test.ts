import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('filterResultsCache', () => {
  beforeEach(() => {
    vi.resetModules();
    // ensure non-browser environment
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    delete (global as any).window;
  });

  it('stores and retrieves ids by hash', async () => {
    const { cacheFilterResult, getCachedFilterResult } = await import('../src/cache/filterResultsCache');
    await cacheFilterResult('abc', [1, 2, 3]);
    const cached = await getCachedFilterResult('abc');
    expect(cached?.ids).toEqual([1, 2, 3]);
  });
});
