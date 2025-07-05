import type {FilterDto} from '../types';
import {isNode} from "@photobank/shared/config";

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

    if (isNode()) {
        // Node.js: use built-in crypto
        const { createHash } = await import('node:crypto');
        return createHash('sha256').update(json).digest('hex');
    }

    if (typeof crypto !== 'undefined' && crypto.subtle) {
        const encoder = new TextEncoder();
        const data = encoder.encode(json);
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        return bufferToHex(hashBuffer);
    }

    // Fallback for older browsers
    const { default: SHA256 } = await import('crypto-js/sha256');
    return SHA256(json).toString();
}

// Helper: convert ArrayBuffer to hex string
function bufferToHex(buffer: ArrayBuffer): string {
    return Array.from(new Uint8Array(buffer))
        .map(b => b.toString(16).padStart(2, '0'))
        .join('');
}
