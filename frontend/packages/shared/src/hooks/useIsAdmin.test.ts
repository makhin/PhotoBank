import { describe, expect, it } from 'vitest';

import { hasAdminRole } from './useIsAdmin';

describe('hasAdminRole', () => {
  it('returns true when Administrator role is present', () => {
    expect(hasAdminRole(['User', 'Administrator'])).toBe(true);
  });

  it('returns false when Administrator role is missing', () => {
    expect(hasAdminRole(['User'])).toBe(false);
  });

  it('returns false when roles are undefined', () => {
    expect(hasAdminRole(undefined)).toBe(false);
  });

  it('returns false when roles are null', () => {
    expect(hasAdminRole(null)).toBe(false);
  });
});
