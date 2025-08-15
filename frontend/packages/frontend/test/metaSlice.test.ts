import { beforeEach, describe, expect, it, vi } from 'vitest';
import { namespacedStorage } from '../src/shared/safeStorage';

const payload = {
  tags: [{ id: 1, name: 't' }],
  persons: [{ id: 2, name: 'p' }],
  paths: [{ id: 3, storageId: 1, path: '/' }],
  storages: [{ id: 4, name: 's' }],
  version: 1,
};

const cacheKey = 'photobank_metadata_cache';
const store = namespacedStorage('meta');

describe('metaSlice', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('clearCache removes cache and resets flag', async () => {
    const { clearCache, default: reducer } = await import('../src/features/meta/model/metaSlice');
    store.set(cacheKey, 'test');
    const state = reducer({ loaded: true } as unknown as Parameters<typeof reducer>[0], clearCache());
    expect(store.get<string>(cacheKey)).toBeNull();
    expect(state.loaded).toBe(false);
  });

  it('loadMetadata returns cached payload', async () => {
    store.set(cacheKey, payload);
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(result.payload).toEqual(payload);
  });

  it('loadMetadata fetches when cache missing', async () => {
    const getAllStorages = vi.fn().mockResolvedValue({ data: payload.storages });
    const getAllTags = vi.fn().mockResolvedValue({ data: payload.tags });
    const getAllPersons = vi.fn().mockResolvedValue({ data: payload.persons });
    const getAllPaths = vi.fn().mockResolvedValue({ data: payload.paths });
    vi.doMock('@photobank/shared/api/photobank', () => ({
      storagesGetAll: getAllStorages,
      tagsGetAll: getAllTags,
      personsGetAll: getAllPersons,
      pathsGetAll: getAllPaths,
    }));
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(getAllStorages).toHaveBeenCalled();
    expect(result.payload).toEqual(payload);
    expect(store.get<typeof payload>(cacheKey)).toEqual(payload);
  });
});
