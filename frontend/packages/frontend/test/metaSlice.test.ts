import { beforeEach, describe, expect, it, vi } from 'vitest';
import { METADATA_CACHE_KEY, METADATA_CACHE_VERSION } from '@photobank/shared/constants';
import { namespacedStorage } from '@photobank/shared/safeStorage';

const payload = {
  tags: [{ id: 1, name: 't' }],
  persons: [{ id: 2, name: 'p' }],
  paths: [{ id: 3, storageId: 1, path: '/' }],
  storages: [{ id: 4, name: 's' }],
  version: METADATA_CACHE_VERSION,
};

const store = namespacedStorage('meta');

describe('metaSlice', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('clearCache removes cache and resets flag', async () => {
    const { clearCache, default: reducer } = await import('../src/features/meta/model/metaSlice');
    store.set(METADATA_CACHE_KEY, 'test');
    const state = reducer({ loaded: true } as unknown as Parameters<typeof reducer>[0], clearCache());
    expect(store.get<string>(METADATA_CACHE_KEY)).toBeNull();
    expect(state.loaded).toBe(false);
  });

  it('loadMetadata returns cached payload', async () => {
    store.set(METADATA_CACHE_KEY, payload);
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(result.payload).toEqual(payload);
  });

  it('loadMetadata fetches when cache missing', async () => {
    const fetchReferenceData = vi.fn().mockResolvedValue({
      tags: payload.tags,
      persons: payload.persons,
      paths: payload.paths,
      storages: payload.storages,
    });
    vi.doMock('@photobank/shared/api/photobank', async (importOriginal) => {
      const actual = await importOriginal<typeof import('@photobank/shared/api/photobank')>();
      return {
        ...actual,
        fetchReferenceData,
      };
    });
    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    const result = await loadMetadata()(dispatch, getState, undefined);
    expect(fetchReferenceData).toHaveBeenCalledTimes(1);
    expect(result.payload).toEqual(payload);
    expect(store.get<typeof payload>(METADATA_CACHE_KEY)).toEqual(payload);
  });

  it('loadMetadata invalidates cache when version mismatches', async () => {
    store.set(METADATA_CACHE_KEY, { ...payload, version: METADATA_CACHE_VERSION + 1 });

    const fetchReferenceData = vi.fn().mockResolvedValue({
      tags: payload.tags,
      persons: payload.persons,
      paths: payload.paths,
      storages: payload.storages,
    });

    vi.doMock('@photobank/shared/api/photobank', async (importOriginal) => {
      const actual = await importOriginal<typeof import('@photobank/shared/api/photobank')>();
      return {
        ...actual,
        fetchReferenceData,
      };
    });

    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();

    const result = await loadMetadata()(dispatch, getState, undefined);

    expect(fetchReferenceData).toHaveBeenCalledTimes(1);
    expect(result.payload).toEqual(payload);
    expect(store.get<typeof payload>(METADATA_CACHE_KEY)).toEqual(payload);
  });

  it('loadMetadata reuses cached payload after first fetch', async () => {
    const fetchReferenceData = vi.fn().mockResolvedValue({
      tags: payload.tags,
      persons: payload.persons,
      paths: payload.paths,
      storages: payload.storages,
    });

    vi.doMock('@photobank/shared/api/photobank', async (importOriginal) => {
      const actual = await importOriginal<typeof import('@photobank/shared/api/photobank')>();
      return {
        ...actual,
        fetchReferenceData,
      };
    });

    const { loadMetadata } = await import('../src/features/meta/model/metaSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();

    const firstCall = await loadMetadata()(dispatch, getState, undefined);
    expect(firstCall.payload).toEqual(payload);

    const secondCall = await loadMetadata()(dispatch, getState, undefined);

    expect(fetchReferenceData).toHaveBeenCalledTimes(1);
    expect(secondCall.payload).toEqual(payload);
    expect(store.get<typeof payload>(METADATA_CACHE_KEY)).toEqual(payload);
  });
});
