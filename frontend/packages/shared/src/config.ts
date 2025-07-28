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
        data = (await res.json()) as { API_BASE_URL?: string };
      }
    } else {
      const fs = await import('fs/promises');
      const url = new URL('../Resources.json', import.meta.url);
      const file = await fs.readFile(url, 'utf-8');
      data = JSON.parse(file) as { API_BASE_URL?: string };
    }
    if (data?.API_BASE_URL) {
      API_BASE_URL = data.API_BASE_URL;
    }
  } catch (err) {
    console.warn('Failed to load Resources.json', err);
  }
}

export function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}
