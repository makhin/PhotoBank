import { describe, expect, it, vi } from 'vitest';

import { fetchReferenceData } from '../src/api/photobank/reference-data';

describe('fetchReferenceData', () => {
  it('collects reference collections concurrently', async () => {
    const fetchTags = vi.fn().mockResolvedValue([{ id: 1, name: 'tag' }]);
    const fetchPersons = vi.fn().mockResolvedValue([{ id: 2, name: 'person' }]);
    const fetchStorages = vi.fn().mockResolvedValue([{ id: 3, name: 'storage' }]);
    const fetchPaths = vi.fn().mockResolvedValue([{ storageId: 3, path: '/a' }]);

    const result = await fetchReferenceData({
      fetchTags,
      fetchPersons,
      fetchStorages,
      fetchPaths,
    });

    expect(result).toEqual({
      tags: [{ id: 1, name: 'tag' }],
      persons: [{ id: 2, name: 'person' }],
      storages: [{ id: 3, name: 'storage' }],
      paths: [{ storageId: 3, path: '/a' }],
    });
    expect(fetchTags).toHaveBeenCalledTimes(1);
    expect(fetchPersons).toHaveBeenCalledTimes(1);
    expect(fetchStorages).toHaveBeenCalledTimes(1);
    expect(fetchPaths).toHaveBeenCalledTimes(1);
  });
});
