import { beforeEach, describe, expect, it, vi } from 'vitest';
import { i18n } from '../src/i18n';

const dictionaryMocks = vi.hoisted(() => ({
  fetchTags: vi.fn(),
  fetchPersons: vi.fn(),
  fetchStorages: vi.fn(),
  fetchPaths: vi.fn(),
}));

vi.mock('../src/services/dictionary', () => dictionaryMocks);

const mockReferenceData = (data: {
  tags?: Array<{ id: number; name: string }>;
  persons?: Array<{ id: number; name: string }>;
  storages?: Array<{ id: number; name: string }>;
  paths?: Array<{ storageId: number; path: string }>;
}) => {
  dictionaryMocks.fetchTags.mockResolvedValue(data.tags ?? []);
  dictionaryMocks.fetchPersons.mockResolvedValue(data.persons ?? []);
  dictionaryMocks.fetchStorages.mockResolvedValue(data.storages ?? []);
  dictionaryMocks.fetchPaths.mockResolvedValue(data.paths ?? []);
};

describe('dictionaries', () => {
  beforeEach(() => {
    vi.resetModules();
    dictionaryMocks.fetchTags.mockReset().mockResolvedValue([]);
    dictionaryMocks.fetchPersons.mockReset().mockResolvedValue([]);
    dictionaryMocks.fetchStorages.mockReset().mockResolvedValue([]);
    dictionaryMocks.fetchPaths.mockReset().mockResolvedValue([]);
  });

  it('getPersonName returns loaded name', async () => {
    mockReferenceData({ persons: [{ id: 1, name: 'John' }] });
    const dict = await import('../src/dictionaries');
    dict.setDictionariesUser(1);
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
    mockReferenceData({
      persons: [
        { id: 1, name: 'Alice' },
        { id: 2, name: 'Bob' },
      ],
      tags: [
        { id: 10, name: 'portrait' },
        { id: 11, name: 'sea' },
      ],
    });
    const dict = await import('../src/dictionaries');
    dict.setDictionariesUser(1);
    await dict.loadDictionaries({} as any);
    expect(dict.findBestPersonId('Alic')).toBe(1);
    expect(dict.findBestTagId('ocean')).toBeUndefined();
  });

  it('getAllStoragesWithPaths returns loaded data', async () => {
    mockReferenceData({
      storages: [{ id: 1, name: 'S1' }],
      paths: [
        { storageId: 1, path: '/a' },
        { storageId: 1, path: '/b' },
      ],
    });
    const dict = await import('../src/dictionaries');
    dict.setDictionariesUser(1);
    await dict.loadDictionaries({} as any);
    expect(dict.getAllStoragesWithPaths()).toEqual([
      { id: 1, name: 'S1', paths: ['/a', '/b'] },
    ]);
  });
});
