export function isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}

export function isNode(): boolean {
    return typeof process !== 'undefined' &&
        process.versions != null &&
        process.versions.node != null;
}

export const API_BASE_URL = isBrowser()
    ? (import.meta as any).env?.API_BASE_URL
    // @ts-ignore
    : (typeof process !== 'undefined' ? process.env.API_BASE_URL : undefined) ?? 'http://localhost:5066';
