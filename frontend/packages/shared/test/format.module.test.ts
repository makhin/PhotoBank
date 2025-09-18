import { format, fromUnixTime, parseISO } from 'date-fns';
import { describe, expect, it } from 'vitest';

import { formatDate, formatDateTime } from '../src/format';

const DATE_FORMAT = 'dd.MM.yyyy';
const DATE_TIME_FORMAT = 'dd.MM.yyyy HH:mm';

describe('format module', () => {
  describe('formatDate', () => {
    it('formats ISO strings', () => {
      const iso = '2024-01-02T03:04:05Z';
      expect(formatDate(iso)).toBe(format(parseISO(iso), DATE_FORMAT));
    });

    it('formats localized strings supported by parse()', () => {
      expect(formatDate('07.06.2024 15:30')).toBe('07.06.2024');
    });

    it('formats timestamps and Date objects', () => {
      const seconds = 1_700_000_000;
      const millis = 1_700_000_000_000;
      const date = new Date('2024-06-07T11:22:33Z');

      expect(formatDate(seconds)).toBe(format(fromUnixTime(seconds), DATE_FORMAT));
      expect(formatDate(millis)).toBe(format(new Date(millis), DATE_FORMAT));
      expect(formatDate(date)).toBe(format(date, DATE_FORMAT));
    });

    it('returns empty string for invalid values', () => {
      expect(formatDate(undefined)).toBe('');
      expect(formatDate(null)).toBe('');
      expect(formatDate('')).toBe('');
      expect(formatDate('   ')).toBe('');
      expect(formatDate('not-a-date')).toBe('');
      expect(formatDate(Number.NaN)).toBe('');
    });
  });

  describe('formatDateTime', () => {
    it('formats ISO strings with time', () => {
      const iso = '2024-01-02T03:04:05Z';
      expect(formatDateTime(iso)).toBe(format(parseISO(iso), DATE_TIME_FORMAT));
    });

    it('formats localized strings supported by parse()', () => {
      expect(formatDateTime('07.06.2024 15:30')).toBe('07.06.2024 15:30');
    });

    it('formats timestamps and Date objects', () => {
      const seconds = 1_700_000_000;
      const millis = 1_700_000_000_000;
      const date = new Date('2024-06-07T11:22:33Z');

      expect(formatDateTime(seconds)).toBe(
        format(fromUnixTime(seconds), DATE_TIME_FORMAT),
      );
      expect(formatDateTime(millis)).toBe(
        format(new Date(millis), DATE_TIME_FORMAT),
      );
      expect(formatDateTime(date)).toBe(format(date, DATE_TIME_FORMAT));
    });

    it('returns empty string for invalid values', () => {
      expect(formatDateTime(undefined)).toBe('');
      expect(formatDateTime(null)).toBe('');
      expect(formatDateTime('')).toBe('');
      expect(formatDateTime('   ')).toBe('');
      expect(formatDateTime('not-a-date')).toBe('');
      expect(formatDateTime(Number.NaN)).toBe('');
    });
  });
});
