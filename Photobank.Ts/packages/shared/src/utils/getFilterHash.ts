import { createHash } from 'node:crypto';
import type { FilterDto } from '../types';

/**
 * Creates a stable hash for a filter. When the filter has `thisDay` set to true,
 * the hash will incorporate the current day of month and month to make the
 * value change daily.
 */
export function getFilterHash(filter: FilterDto): string {
    const now = new Date();

    const entries: [string, unknown][] = [];
    for (const key in filter) {
        const value = filter[key as keyof FilterDto];
        if (value === undefined) continue;

        if (key === 'thisDay' && value) {
            entries.push(['day', now.getDate()], ['month', now.getMonth() + 1]);
        } else {
            entries.push([key, value]);
        }
    }

    entries.sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0));

    let json = '{';
    for (let i = 0; i < entries.length; i++) {
        const [k, v] = entries[i];
        json += JSON.stringify(k) + ':' + JSON.stringify(v);
        if (i < entries.length - 1) json += ',';
    }
    json += '}';

    return createHash('md5').update(json).digest('hex');
}