export type SafeStorage = {
  get: <T>(key: string) => T | null;
  set: <T>(key: string, value: T) => void;
  remove: (key: string) => void;
};

function createSafe(storage: Storage | undefined): SafeStorage {
  return {
    get<T>(key: string): T | null {
      if (!storage) return null;
      try {
        const raw = storage.getItem(key);
        return raw ? (JSON.parse(raw) as T) : null;
      } catch {
        return null;
      }
    },
    set<T>(key: string, value: T): void {
      if (!storage) return;
      try {
        storage.setItem(key, JSON.stringify(value));
      } catch {
        // ignore
      }
    },
    remove(key: string): void {
      if (!storage) return;
      try {
        storage.removeItem(key);
      } catch {
        // ignore
      }
    },
  };
}

function getStorage(type: 'localStorage' | 'sessionStorage'): Storage | undefined {
  try {
    return typeof window !== 'undefined' ? (window as any)[type] : undefined;
  } catch {
    return undefined;
  }
}

export const safeLocalStorage = createSafe(getStorage('localStorage'));
export const safeSessionStorage = createSafe(getStorage('sessionStorage'));

export const safeStorage = safeLocalStorage;

export const namespacedStorage = (ns: string, type: 'local' | 'session' = 'local'): SafeStorage => {
  const base = type === 'local' ? safeLocalStorage : safeSessionStorage;
  return {
    get: <T>(key: string) => base.get<T>(`${ns}:${key}`),
    set: <T>(key: string, value: T) => base.set(`${ns}:${key}`, value),
    remove: (key: string) => base.remove(`${ns}:${key}`),
  };
};
