import { describe, expect, it } from 'vitest';
import { parseISO } from 'date-fns';

import { serializeFilter, deserializeFilter } from '../src/shared/lib/filter-url';
import type { FormData } from '../src/features/filter/lib/form-schema';

const decode = (value: string) => {
  const json = Buffer.from(value, 'base64').toString('utf-8');
  return JSON.parse(json) as Record<string, unknown>;
};

describe('filter-url serialization', () => {
  it('serializes date without timezone shift', () => {
    const date = parseISO('2015-12-17T00:00:00.000Z');
    const data: FormData = { dateFrom: date };

    const serialized = serializeFilter(data);
    const parsed = decode(serialized);

    expect(parsed.dateFrom).toBe('2015-12-17T00:00:00.000Z');
  });

  it('deserializes date without timezone shift', () => {
    const date = parseISO('2015-12-17T00:00:00.000Z');
    const data: FormData = { dateFrom: date };

    const serialized = serializeFilter(data);
    const deserialized = deserializeFilter(serialized);

    expect(deserialized?.dateFrom?.toISOString()).toBe('2015-12-17T00:00:00.000Z');
  });
});
