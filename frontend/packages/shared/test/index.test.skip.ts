import { format, parseISO } from 'date-fns';
import { describe, expect, it } from 'vitest';

import { formatDate, getGenderText } from '../src';

// FormatDate tests

describe('formatDate', () => {
  it('returns formatted date for valid ISO string', () => {
    const input = '2024-01-02T03:04:05Z';
    const expected = format(parseISO(input), 'dd.MM.yyyy, HH:mm');
    const result = formatDate(input);
    expect(result).toBe(expected);
  });

  it('returns fallback for undefined input', () => {
    expect(formatDate()).toBe('не указана дата');
  });

  it('returns fallback for invalid input', () => {
    expect(formatDate('not-a-date')).toBe('неверный формат даты');
  });
});

describe('getGenderText', () => {
  it('returns text for male', () => {
    expect(getGenderText(true)).toBe('Муж');
  });

  it('returns text for female', () => {
    expect(getGenderText(false)).toBe('Жен');
  });

  it('returns fallback for undefined', () => {
    expect(getGenderText(undefined)).toBe('не указан пол');
  });
});
