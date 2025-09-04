import { beforeEach, describe, expect, it, vi } from 'vitest';
import { i18n } from '../src/i18n';

describe('dictionaries', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('getPersonName returns loaded name', async () => {
    const getAllPersons = vi.fn().mockResolvedValue([{ id: 1, name: 'John' }]);
    const getAllTags = vi.fn().mockResolvedValue([]);
    const getAllStorages = vi.fn().mockResolvedValue([]);
    const getAllPaths = vi.fn().mockResolvedValue([]);
    vi.doMock('../src/services/dictionary', () => ({
      fetchPersons: getAllPersons,
      fetchTags: getAllTags,
      fetchStorages: getAllStorages,
      fetchPaths: getAllPaths,
    }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries({} as any);
    expect(dict.getPersonName(1)).toBe('John');
  });

  it('getPersonName falls back to id', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getPersonName(99)).toBe('ID 99');
  });

  it('getPersonName returns unknown label for null', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getPersonName(null)).toBe(i18n.t('en', 'unknown-person'));
  });

  it('getTagName and getStorageName fall back', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getTagName(5)).toBe('#5');
    expect(dict.getStorageName(7)).toBe('ID 7');
  });

  it('findBestPersonId and findBestTagId return closest match', async () => {
    const getAllPersons = vi.fn().mockResolvedValue([
      { id: 1, name: 'Alice' },
      { id: 2, name: 'Bob' },
    ]);
    const getAllTags = vi.fn().mockResolvedValue([
      { id: 10, name: 'portrait' },
      { id: 11, name: 'sea' },
    ]);
    const getAllStorages = vi.fn().mockResolvedValue([]);
    const getAllPaths = vi.fn().mockResolvedValue([]);
    vi.doMock('../src/services/dictionary', () => ({
      fetchPersons: getAllPersons,
      fetchTags: getAllTags,
      fetchStorages: getAllStorages,
      fetchPaths: getAllPaths,
    }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries({} as any);
    expect(dict.findBestPersonId('Alic')).toBe(1);
    expect(dict.findBestTagId('ocean')).toBeUndefined();
  });

  it('getAllStoragesWithPaths returns loaded data', async () => {
    const getAllStorages = vi.fn().mockResolvedValue([{ id: 1, name: 'S1' }]);
    const getAllPaths = vi.fn().mockResolvedValue([
      { storageId: 1, path: '/a' },
      { storageId: 1, path: '/b' },
    ]);
    vi.doMock('../src/services/dictionary', () => ({
      fetchPersons: vi.fn().mockResolvedValue([]),
      fetchTags: vi.fn().mockResolvedValue([]),
      fetchStorages: getAllStorages,
      fetchPaths: getAllPaths,
    }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries({} as any);
    expect(dict.getAllStoragesWithPaths()).toEqual([
      { id: 1, name: 'S1', paths: ['/a', '/b'] },
    ]);
  });
});
