import { describe, it, expect, vi } from 'vitest';
import { formatDate, getGenderText } from '../src';

// FormatDate tests

describe('formatDate', () => {
  it('returns formatted date for valid ISO string', () => {
    const result = formatDate('2024-01-02T03:04:05Z');
    expect(result).toBe('02.01.2024, 04:04');
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
