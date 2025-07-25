import { describe, it, expect, vi } from 'vitest';
import { getFilterHash } from '../src';
import type { FilterDto } from '../src/generated';

describe('getFilterHash', () => {
  it('returns stable hash for identical filters', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-05-05T12:00:00Z'));
    const filter1: FilterDto = { caption: 'a', thisDay: true };
    const filter2: FilterDto = { thisDay: true, caption: 'a' };
    const hash1 = await getFilterHash(filter1);
    const hash2 = await getFilterHash(filter2);
    expect(hash1).toBe(hash2);
    vi.useRealTimers();
  });

  it('changes when day changes for thisDay filter', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-05-05T12:00:00Z'));
    const filter: FilterDto = { thisDay: true };
    const hash1 = await getFilterHash(filter);
    vi.setSystemTime(new Date('2024-05-06T12:00:00Z'));
    const hash2 = await getFilterHash(filter);
    vi.useRealTimers();
    expect(hash1).not.toBe(hash2);
  });
});

