import { describe, expect, it } from 'vitest';

import {
  formatDate,
  formatDateTime,
  formatBytes,
  formatDuration,
  formatBool,
  formatList,
} from '../src/format';

const locale = 'ru-RU';

describe('format module', () => {
  it('formats date', () => {
    const d = new Date('2024-01-02T03:04:05Z');
    const expected = new Intl.DateTimeFormat(locale).format(d);
    expect(formatDate(d)).toBe(expected);
  });

  it('formats date time', () => {
    const d = new Date('2024-01-02T03:04:05Z');
    const expected = new Intl.DateTimeFormat(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })
      .format(d)
      .replace(',', '');
    expect(formatDateTime(d)).toBe(expected);
  });

  it('formats bytes', () => {
    expect(formatBytes(1_048_576)).toBe('1 MB');
    expect(formatBytes(1_500_000)).toBe('1,4 MB');
  });

  it('formats duration', () => {
    expect(formatDuration(3600_000 + 23 * 60_000)).toBe('1h 23m');
    expect(formatDuration(42_000)).toBe('42s');
  });

  it('formats boolean', () => {
    expect(formatBool(true)).toBe('Yes');
    expect(formatBool(false)).toBe('No');
    expect(formatBool(null)).toBe('No');
  });

  it('formats list', () => {
    expect(formatList(['a', 'b', 'c', 'd'], 2)).toEqual({
      visible: ['a', 'b'],
      hidden: 2,
    });
  });
});
