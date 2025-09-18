import { format, fromUnixTime } from 'date-fns';
import { describe, expect, it } from 'vitest';

import { formatDate } from '../src/index';

const DATE_FORMAT = 'dd.MM.yyyy';

describe('formatDate (legacy export)', () => {
  it('formats ISO strings through date-fns', () => {
    const value = '2024-01-02T03:04:05Z';
    expect(formatDate(value)).toBe(format(new Date(value), DATE_FORMAT));
  });

  it('formats localized strings supported by parse()', () => {
    expect(formatDate('07.06.2024')).toBe('07.06.2024');
  });

  it('formats unix timestamps in seconds and milliseconds', () => {
    const seconds = 1_700_000_000;
    const millis = 1_700_000_000_000;

    expect(formatDate(seconds)).toBe(format(fromUnixTime(seconds), DATE_FORMAT));
    expect(formatDate(millis)).toBe(format(new Date(millis), DATE_FORMAT));
  });

  it('formats Date instances with the same rules', () => {
    const value = new Date('2024-05-06T07:08:09Z');
    expect(formatDate(value)).toBe(format(value, DATE_FORMAT));
  });

  it('keeps legacy fallback messages', () => {
    expect(formatDate(undefined)).toBe('не указана дата');
    expect(formatDate(null)).toBe('не указана дата');
    expect(formatDate('')).toBe('не указана дата');
    expect(formatDate('   ')).toBe('не указана дата');
    expect(formatDate(Number.NaN)).toBe('неверный формат даты');
    expect(formatDate('definitely-not-a-date')).toBe('неверный формат даты');
  });
});
