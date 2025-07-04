export const isBrowser = typeof window !== 'undefined';

export const API_BASE_URL = isBrowser
    ? (import.meta as any).env?.VITE_API_BASE_URL
    // @ts-ignore
    : (typeof process !== 'undefined' ? process.env.API_BASE_URL : undefined) ?? 'http://localhost:5066';
