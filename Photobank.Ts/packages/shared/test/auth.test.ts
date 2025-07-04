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
    const auth = await import('../src/api/auth');
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
    const auth = await import('../src/api/auth');
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
    const auth = await import('../src/api/auth');
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
    const auth = await import('../src/api/auth');
    expect(auth.getAuthToken()).toBe('init');
  });

  it('login posts credentials and saves token', async () => {
    const postMock = vi.fn().mockResolvedValue({ data: { token: 'res' } });
    vi.doMock('../src/api/client', () => ({ apiClient: { post: postMock } }));
    const auth = await import('../src/api/auth');
    const result = await auth.login({ email: 'e', password: 'p' });
    expect(postMock).toHaveBeenCalledWith('/auth/login', { email: 'e', password: 'p' });
    expect(result).toEqual({ token: 'res' });
    expect(auth.getAuthToken()).toBe('res');
  });

  it('register posts user info', async () => {
    const postMock = vi.fn().mockResolvedValue({});
    vi.doMock('../src/api/client', () => ({ apiClient: { post: postMock } }));
    const auth = await import('../src/api/auth');
    await auth.register({ email: 'e', password: 'p' });
    expect(postMock).toHaveBeenCalledWith('/auth/register', { email: 'e', password: 'p' });
  });

  it('getCurrentUser fetches user', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: { email: 'a@b.c' } });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const auth = await import('../src/api/auth');
    const user = await auth.getCurrentUser();
    expect(getMock).toHaveBeenCalledWith('/auth/user');
    expect(user).toEqual({ email: 'a@b.c' });
  });

  it('updateUser sends data', async () => {
    const putMock = vi.fn().mockResolvedValue({});
    vi.doMock('../src/api/client', () => ({ apiClient: { put: putMock } }));
    const auth = await import('../src/api/auth');
    await auth.updateUser({ phoneNumber: '123' });
    expect(putMock).toHaveBeenCalledWith('/auth/user', { phoneNumber: '123' });
  });

  it('getUserClaims fetches claims', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ type: 't', value: 'v' }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const auth = await import('../src/api/auth');
    const claims = await auth.getUserClaims();
    expect(getMock).toHaveBeenCalledWith('/auth/claims');
    expect(claims).toEqual([{ type: 't', value: 'v' }]);
  });
});
