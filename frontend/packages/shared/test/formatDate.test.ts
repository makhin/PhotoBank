import { describe, expect, it } from 'vitest';

import { formatDate } from '../src/index';

const options: Intl.DateTimeFormatOptions = {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
};

describe('formatDate (legacy export)', () => {
  it('formats ISO strings using ru-RU locale', () => {
    const value = '2024-01-02T03:04:05Z';
    const expected = new Intl.DateTimeFormat('ru-RU', options).format(
      new Date(value),
    );

    expect(formatDate(value)).toBe(expected);
  });

  it('formats Date instances with the same rules', () => {
    const value = new Date('2024-05-06T07:08:09Z');
    const expected = new Intl.DateTimeFormat('ru-RU', options).format(value);

    expect(formatDate(value)).toBe(expected);
  });

  it('keeps legacy fallback messages', () => {
    expect(formatDate(undefined)).toBe('не указана дата');
    expect(formatDate(null)).toBe('не указана дата');
    expect(formatDate('')).toBe('не указана дата');
    expect(formatDate('definitely-not-a-date')).toBe('неверный формат даты');
  });
});
