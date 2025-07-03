import { createHash } from 'node:crypto';
import type { FilterDto } from '../types';

function sortObject<T extends Record<string, unknown>>(obj: T): Record<string, unknown> {
  return Object.keys(obj)
    .sort()
    .reduce<Record<string, unknown>>((acc, key) => {
      acc[key] = obj[key];
      return acc;
    }, {});
}

/**
 * Creates a stable hash for a filter. When the filter has `thisDay` set to true,
 * the hash will incorporate the current day of month and month to make the
 * value change daily.
 */
export function getFilterHash(filter: FilterDto): string {
  const clean: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(filter)) {
    if (v !== undefined) clean[k] = v;
  }

  if (clean.thisDay) {
    const now = new Date();
    clean.day = now.getDate();
    clean.month = now.getMonth() + 1;
    delete clean.thisDay;
  }

  const json = JSON.stringify(sortObject(clean));
  return createHash('md5').update(json).digest('hex');
}

