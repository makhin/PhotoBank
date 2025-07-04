import type {FilterDto} from '../types';
import {isBrowser, isNode} from "@photobank/shared/config";

/**
 * Creates a stable hash for a filter. Works in both Node.js and browser.
 * Uses SHA-256 (secure and supported everywhere).
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

    const json = JSON.stringify(normalized);
    const encoder = new TextEncoder();
    const data = encoder.encode(json);

    if (isNode()) {
        // Node.js: use built-in crypto
        const { createHash } = await import('node:crypto');
        return createHash('sha256').update(data).digest('hex');
    } else if (isBrowser() && crypto.subtle) {
        // Browser: use Web Crypto API
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        return bufferToHex(hashBuffer);
    } else {
        throw new Error('No suitable crypto API found');
    }
}

// Helper: convert ArrayBuffer to hex string
function bufferToHex(buffer: ArrayBuffer): string {
    return Array.from(new Uint8Array(buffer))
        .map(b => b.toString(16).padStart(2, '0'))
        .join('');
}
