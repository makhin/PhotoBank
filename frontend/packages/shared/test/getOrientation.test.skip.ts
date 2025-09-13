import { describe, it, expect } from 'vitest';

import { getOrientation } from '../src/utils/getOrientation';

describe('getOrientation', () => {
  it('returns friendly text for known values', () => {
    expect(getOrientation(1)).toBe('Normal');
    expect(getOrientation(6)).toBe('Rotate 90°');
  });

  it('handles unknown values', () => {
    expect(getOrientation(99)).toBe('unknown (99)');
  });

  it('handles undefined', () => {
    expect(getOrientation()).toBe('unknown');
  });
});
