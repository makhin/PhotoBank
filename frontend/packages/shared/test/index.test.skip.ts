import { format, parseISO } from 'date-fns';
import { describe, expect, it } from 'vitest';

import { formatDate, formatGender, resolveGender } from '../src';

// FormatDate tests

describe('formatDate', () => {
  it('returns formatted date for valid ISO string', () => {
    const input = '2024-01-02T03:04:05Z';
    const expected = format(parseISO(input), 'dd.MM.yyyy, HH:mm');
    const result = formatDate(input);
    expect(result).toBe(expected);
  });

  it('returns empty string for undefined input', () => {
    expect(formatDate()).toBe('');
  });

  it('returns empty string for invalid input', () => {
    expect(formatDate('not-a-date')).toBe('');
  });
});

describe('gender helpers', () => {
  const labels = {
    male: 'Male',
    female: 'Female',
    unknown: 'Unknown',
  } as const;

  it('resolves keys correctly', () => {
    expect(resolveGender(true)).toBe('male');
    expect(resolveGender(false)).toBe('female');
    expect(resolveGender(undefined)).toBe('unknown');
  });

  it('formats using provided labels', () => {
    expect(formatGender(true, labels)).toBe('Male');
    expect(formatGender(false, labels)).toBe('Female');
    expect(formatGender(undefined, labels)).toBe('Unknown');
  });
});
