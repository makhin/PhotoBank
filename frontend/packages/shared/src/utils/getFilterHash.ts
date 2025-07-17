import type {FilterDto} from '../types';
import objectHash from 'object-hash';

/**
 * Creates a stable hash for a filter. Works in both Node.js and browser.
 * Uses the `object-hash` package for consistent results.
 */
export async function getFilterHash(filter: FilterDto): Promise<string> {
    const now = new Date();

    // Normalize and sort filter keys
    const normalized: Record<string, unknown> = {};
    for (const key of Object.keys(filter).sort()) {
        const value = filter[key as keyof FilterDto];
        if (value === undefined) continue;

        if (key === 'thisDay' && value) {
            normalized.day = now.getDate();
            normalized.month = now.getMonth() + 1;
        } else {
            normalized[key] = value;
        }
    }

    return objectHash(normalized);
}
