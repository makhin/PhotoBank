export function isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}

export function isNode(): boolean {
    return typeof process !== 'undefined' &&
        process.versions != null &&
        process.versions.node != null;
}

export const API_BASE_URL = isBrowser()
    ? (import.meta as any).env?.VITE_API_BASE_URL
    // @ts-ignore
    : (typeof process !== 'undefined' ? process.env.API_BASE_URL : undefined) ?? 'http://192.168.1.45:5066';
