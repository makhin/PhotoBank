export type SafeStorage = {
  get: <T>(key: string) => T | null;
  set: <T>(key: string, value: T) => void;
  remove: (key: string) => void;
};

function isStorageAvailable(): boolean {
  try {
    return typeof window !== 'undefined' && !!window.localStorage;
  } catch {
    return false;
  }
}

export const safeStorage: SafeStorage = {
  get<T>(key: string): T | null {
    if (!isStorageAvailable()) return null;
    try {
      const raw = window.localStorage.getItem(key);
      return raw ? (JSON.parse(raw) as T) : null;
    } catch {
      return null;
    }
  },
  set<T>(key: string, value: T): void {
    if (!isStorageAvailable()) return;
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch {
      // ignore
    }
  },
  remove(key: string): void {
    if (!isStorageAvailable()) return;
    try {
      window.localStorage.removeItem(key);
    } catch {
      // ignore
    }
  },
};

export const namespacedStorage = (ns: string): SafeStorage => ({
  get: <T>(key: string) => safeStorage.get<T>(`${ns}:${key}`),
  set: <T>(key: string, value: T) => safeStorage.set(`${ns}:${key}`, value),
  remove: (key: string) => safeStorage.remove(`${ns}:${key}`),
});
