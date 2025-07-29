import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('dictionaries', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('getPersonName returns loaded name', async () => {
    const getAllPersons = vi.fn().mockResolvedValue([{ id: 1, name: 'John' }]);
    const getAllTags = vi.fn().mockResolvedValue([]);
    vi.doMock('../src/generated', () => ({
      PersonsService: { getApiPersons: getAllPersons },
      TagsService: { getApiTags: getAllTags },
    }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries();
    expect(dict.getPersonName(1)).toBe('John');
  });

  it('getPersonName falls back to id', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getPersonName(99)).toBe('ID 99');
  });

  it('getPersonName returns unknown label for null', async () => {
    const dict = await import('../src/dictionaries');
    expect(dict.getPersonName(null)).toBe('Неизвестный');
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
    vi.doMock('../src/generated', () => ({
      PersonsService: { getApiPersons: getAllPersons },
      TagsService: { getApiTags: getAllTags },
    }));
    const dict = await import('../src/dictionaries');
    await dict.loadDictionaries();
    expect(dict.findBestPersonId('Alic')).toBe(1);
    expect(dict.findBestTagId('ocean')).toBeUndefined();
  });
});
