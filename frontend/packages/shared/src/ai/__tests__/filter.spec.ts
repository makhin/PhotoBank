import { describe, expect, it } from 'vitest';

import { PhotoFilterSchema } from '../filter';

describe('PhotoFilterSchema date transformations', () => {
  it('parses ISO datetime strings into Date instances', () => {
    const isoFrom = '2024-06-07T08:09:10Z';
    const isoTo = '2024-06-08T09:10:11Z';
    const expectedFrom = new Date(isoFrom).toISOString();
    const expectedTo = new Date(isoTo).toISOString();

    const result = PhotoFilterSchema.parse({
      dateFrom: isoFrom,
      dateTo: isoTo,
    });

    expect(result.dateFrom).toBeInstanceOf(Date);
    expect(result.dateFrom?.toISOString()).toBe(expectedFrom);
    expect(result.dateTo).toBeInstanceOf(Date);
    expect(result.dateTo?.toISOString()).toBe(expectedTo);
  });

  it('parses day strings into UTC midnight Date instances', () => {
    const result = PhotoFilterSchema.parse({
      dateFrom: '2024-06-07',
      dateTo: '2024-06-08',
    });

    expect(result.dateFrom).toBeInstanceOf(Date);
    expect(result.dateFrom?.toISOString()).toBe('2024-06-07T00:00:00.000Z');
    expect(result.dateTo).toBeInstanceOf(Date);
    expect(result.dateTo?.toISOString()).toBe('2024-06-08T00:00:00.000Z');
  });

  it('returns null for invalid ISO datetime strings', () => {
    const result = PhotoFilterSchema.parse({
      dateFrom: '2024-13-01T00:00:00Z',
      dateTo: '2024-02-30T12:00:00Z',
    });

    expect(result.dateFrom).toBeNull();
    expect(result.dateTo).toBeNull();
  });

  it('returns null for invalid day strings', () => {
    const result = PhotoFilterSchema.parse({
      dateFrom: '2024-13-01',
      dateTo: 'not-a-date',
    });

    expect(result.dateFrom).toBeNull();
    expect(result.dateTo).toBeNull();
  });
});
