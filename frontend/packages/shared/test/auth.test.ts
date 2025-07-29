import { beforeEach, describe, expect, it, vi } from 'vitest';

// helper to create a minimal localStorage mock
const createStorage = (initial: Record<string, string> = {}) => {
  const store: Record<string, string | undefined> = { ...initial };
  return {
    getItem: (k: string) => (k in store ? store[k]! : null),
    setItem: (k: string, v: string) => {
      store[k] = v;
    },
    removeItem: (k: string) => {
      delete store[k];
    },
  };
};

describe('auth utilities', () => {
  beforeEach(() => {
    vi.resetModules();
    // ensure a clean global
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    delete (global as any).window;
    // @ts-ignore
    delete (global as any).localStorage;
  });

  it('sets and gets token without browser', async () => {
    const auth = await import('../src/auth');
    auth.setAuthToken('abc');
    expect(auth.getAuthToken()).toBe('abc');
  });

  it('persists token to localStorage in browser', async () => {
    // @ts-ignore
    const storage = createStorage();
    // @ts-ignore
    global.window = { localStorage: storage };
    // @ts-ignore
    global.localStorage = storage;
    const auth = await import('../src/auth');
    auth.setAuthToken('token');
    expect(auth.getAuthToken()).toBe('token');
    expect(global.window.localStorage.getItem('photobank_token')).toBe('token');
  });

  it('clears token and storage', async () => {
    // @ts-ignore
    const storage = createStorage();
    // @ts-ignore
    global.window = { localStorage: storage };
    // @ts-ignore
    global.localStorage = storage;
    const auth = await import('../src/auth');
    auth.setAuthToken('tok');
    auth.clearAuthToken();
    expect(auth.getAuthToken()).toBeNull();
    expect(global.window.localStorage.getItem('photobank_token')).toBeNull();
  });

  it('loads token from storage on import', async () => {
    // @ts-ignore
    const storage = createStorage({ photobank_token: 'init' });
    // @ts-ignore
    global.window = { localStorage: storage };
    // @ts-ignore
    global.localStorage = storage;
    const auth = await import('../src/auth');
    expect(auth.getAuthToken()).toBe('init');
  });

});
