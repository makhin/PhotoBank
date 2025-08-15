import { beforeEach, describe, expect, it } from 'vitest';
import { safeStorage } from '@photobank/shared/safeStorage';

const defineWindow = () => {
  const store: Record<string, string> = {};
  Object.defineProperty(globalThis, 'window', {
    value: {
      localStorage: {
        getItem: (key: string) => store[key] ?? null,
        setItem: (key: string, value: string) => {
          store[key] = value;
        },
        removeItem: (key: string) => {
          delete store[key];
        },
        clear: () => {
          Object.keys(store).forEach((k) => delete store[k]);
        },
      },
    },
    writable: true,
    configurable: true,
  });
};

describe('safeStorage', () => {
  beforeEach(() => {
    defineWindow();
  });

  it('stores and retrieves values', () => {
    safeStorage.set('a', { b: 1 });
    expect(safeStorage.get<{ b: number }>('a')).toEqual({ b: 1 });
    safeStorage.remove('a');
    expect(safeStorage.get('a')).toBeNull();
  });

  it('returns null on broken JSON', () => {
    window.localStorage.setItem('broken', '{');
    expect(safeStorage.get('broken')).toBeNull();
  });

  it('returns null when window is missing', () => {
    const win = (globalThis as { window?: unknown }).window;
    // Remove window to simulate server environment
    Reflect.deleteProperty(globalThis, 'window');
    expect(safeStorage.get('any')).toBeNull();
    // Restore window
    Object.defineProperty(globalThis, 'window', {
      value: win,
      writable: true,
      configurable: true,
    });
  });
});
