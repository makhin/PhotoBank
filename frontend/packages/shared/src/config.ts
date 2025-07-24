// Shared runtime configuration loaded from Resources.json

let API_BASE_URL = "http://localhost:5066";

export function getApiBaseUrl(): string {
  return API_BASE_URL;
}

export async function loadResources(): Promise<void> {
  try {
    let data: { API_BASE_URL?: string } | undefined;
    if (isBrowser()) {
      const res = await fetch('/Resources.json');
      if (res.ok) {
        data = await res.json();
      }
    } else {
      const fs = await import('fs/promises');
      const path = await import('path');
      const file = await fs.readFile(path.resolve(__dirname, '../Resources.json'), 'utf-8');
      data = JSON.parse(file);
    }
    if (data?.API_BASE_URL) {
      API_BASE_URL = data.API_BASE_URL;
    }
  } catch (err) {
    console.warn('Failed to load Resources.json', err);
  }
}

export const GOOGLE_API_KEY: string | undefined = (() => {
  if (typeof import.meta !== 'undefined' && (import.meta as any).env?.VITE_GOOGLE_API_KEY) {
    return (import.meta as any).env.VITE_GOOGLE_API_KEY as string;
  }
  if (typeof process !== 'undefined') {
    return process.env.VITE_GOOGLE_API_KEY ?? process.env.GOOGLE_API_KEY;
  }
  return undefined;
})();

export function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}
