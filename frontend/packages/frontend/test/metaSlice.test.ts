import { beforeEach, describe, expect, it, vi } from 'vitest';

const payload = {
  tags: [{ id: 1, name: 't' }],
  persons: [{ id: 2, name: 'p' }],
  paths: [{ id: 3, storageId: 1, path: '/' }],
  storages: [{ id: 4, name: 's' }],
  version: 1,
};

const cacheKey = 'photobank_metadata_cache';

describe('metaSlice', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('clearCache removes cache and resets flag', async () => {
    const { clearCache, default: reducer } = await import('../src/features/meta/model/metaSlice');
    localStorage.setItem(cacheKey, 'test');
    const state = reducer({ loaded: true } as any, clearCache());
    expect(localStorage.getItem(cacheKey)).toBeNull();
    expect(state.loaded).toBe(false);
  });

  it('loadMetadata returns cached payload', async () => {
    localStorage.setItem(cacheKey, JSON.stringify(payload));
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(result.payload).toEqual(payload);
  });

  it('loadMetadata fetches when cache missing', async () => {
    const getAllStorages = vi.fn().mockResolvedValue(payload.storages);
    const getAllTags = vi.fn().mockResolvedValue(payload.tags);
    const getAllPersons = vi.fn().mockResolvedValue(payload.persons);
    const getAllPaths = vi.fn().mockResolvedValue(payload.paths);
    vi.doMock('@photobank/shared/api', () => ({
      getAllStorages,
      getAllTags,
      getAllPersons,
      getAllPaths,
    }));
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(getAllStorages).toHaveBeenCalled();
    expect(result.payload).toEqual(payload);
    expect(JSON.parse(localStorage.getItem(cacheKey)!)).toEqual(payload);
  });
});
