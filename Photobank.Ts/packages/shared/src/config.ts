export const API_BASE_URL: string = "http://192.168.1.45:5066"

export function isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}

export function isNode(): boolean {
    return typeof process !== 'undefined' &&
        process.versions != null &&
        process.versions.node != null;
}