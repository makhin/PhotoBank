import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('dictionaries', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('getPersonName returns loaded name', async () => {
    const getAllPersons = vi.fn().mockResolvedValue([{ id: 1, name: 'John' }]);
    vi.doMock('../src/api', () => ({ getAllPersons }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries();
    expect(dict.getPersonName(1)).toBe('John');
  });

  it('getPersonName falls back to id', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getPersonName(99)).toBe('ID 99');
  });

  it('getTagName and getStorageName fall back', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getTagName(5)).toBe('#5');
    expect(dict.getStorageName(7)).toBe('ID 7');
  });
});
