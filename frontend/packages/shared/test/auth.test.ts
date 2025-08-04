import { beforeEach, describe, expect, it, vi } from 'vitest';

// helper to create a minimal localStorage mock
const createStorage = (initial: Record<string, string> = {}): Storage => {
  const store: Record<string, string> = { ...initial };
  return {
    getItem: (k: string) => (k in store ? store[k] : null),
    setItem: (k: string, v: string) => {
      store[k] = v;
    },
    removeItem: (k: string) => {
      Reflect.deleteProperty(store, k);
    },
    clear: () => {
      Object.keys(store).forEach((k) => Reflect.deleteProperty(store, k));
    },
    key: (index: number) => Object.keys(store)[index] ?? null,
    get length() {
      return Object.keys(store).length;
    },
  };
};

type GlobalWithStorage = typeof globalThis & {
  window?: { localStorage: Storage };
  localStorage?: Storage;
};

const globalWithStorage = globalThis as GlobalWithStorage;

describe('auth utilities', () => {
  beforeEach(() => {
    vi.resetModules();
    // ensure a clean global
    delete globalWithStorage.window;
    delete globalWithStorage.localStorage;
  });

  it('sets and gets token without browser', async () => {
    const auth = await import('../src/auth');
    auth.setAuthToken('abc');
    expect(auth.getAuthToken()).toBe('abc');
  });

  it('persists token to localStorage in browser', async () => {
    const storage = createStorage();
    globalWithStorage.window = { localStorage: storage };
    globalWithStorage.localStorage = storage;
    const auth = await import('../src/auth');
    auth.setAuthToken('token');
    expect(auth.getAuthToken()).toBe('token');
    expect(globalWithStorage.window.localStorage.getItem('photobank_token')).toBe('token');
  });

  it('clears token and storage', async () => {
    const storage = createStorage();
    globalWithStorage.window = { localStorage: storage };
    globalWithStorage.localStorage = storage;
    const auth = await import('../src/auth');
    auth.setAuthToken('tok');
    auth.clearAuthToken();
    expect(auth.getAuthToken()).toBeNull();
    expect(globalWithStorage.window.localStorage.getItem('photobank_token')).toBeNull();
  });

  it('loads token from storage on import', async () => {
    const storage = createStorage({ photobank_token: 'init' });
    globalWithStorage.window = { localStorage: storage };
    globalWithStorage.localStorage = storage;
    const auth = await import('../src/auth');
    expect(auth.getAuthToken()).toBe('init');
  });

});
