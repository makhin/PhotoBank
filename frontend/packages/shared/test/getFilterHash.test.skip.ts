import { describe, expect, it, vi } from 'vitest';

import { getFilterHash } from '../src';
import type { FilterDto } from '../src/api/photobank';

describe('getFilterHash', () => {
  it('returns stable hash for identical filters', () => {
    const filter1: FilterDto = { caption: 'a', thisDay: { day: 5, month: 5 } };
    const filter2: FilterDto = { thisDay: { month: 5, day: 5 }, caption: 'a' };
    const hash1 = getFilterHash(filter1);
    const hash2 = getFilterHash(filter2);
    expect(hash1).toBe(hash2);
  });

  it('changes when day changes for thisDay filter', () => {
    const filter1: FilterDto = { thisDay: { day: 5, month: 5 } };
    const filter2: FilterDto = { thisDay: { day: 6, month: 5 } };
    const hash1 = getFilterHash(filter1);
    const hash2 = getFilterHash(filter2);
    expect(hash1).not.toBe(hash2);
  });
});

