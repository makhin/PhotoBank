import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('api helpers', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('searchPhotos posts filter', async () => {
    const postMock = vi.fn().mockResolvedValue({ data: { count: 1 } });
    vi.doMock('../src/api/client', () => ({ apiClient: { post: postMock } }));
    const { searchPhotos } = await import('../src/api/photos');
    const res = await searchPhotos({ thisDay: true } as any);
    expect(postMock).toHaveBeenCalledWith('/photos/search', { thisDay: true });
    expect(res).toEqual({ count: 1 });
  });

  it('getPhotoById requests by id', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: { id: 5 } });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getPhotoById } = await import('../src/api/photos');
    const res = await getPhotoById(5);
    expect(getMock).toHaveBeenCalledWith('/photos/5');
    expect(res).toEqual({ id: 5 });
  });

  it('getAllPersons fetches persons', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ id: 1 }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getAllPersons } = await import('../src/api/persons');
    const res = await getAllPersons();
    expect(getMock).toHaveBeenCalledWith('/persons');
    expect(res).toEqual([{ id: 1 }]);
  });

  it('getAllPaths fetches paths', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ storageId: 1 }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getAllPaths } = await import('../src/api/paths');
    const res = await getAllPaths();
    expect(getMock).toHaveBeenCalledWith('/paths');
    expect(res).toEqual([{ storageId: 1 }]);
  });

  it('getAllStorages fetches storages', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ id: 2 }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getAllStorages } = await import('../src/api/storages');
    const res = await getAllStorages();
    expect(getMock).toHaveBeenCalledWith('/storages');
    expect(res).toEqual([{ id: 2 }]);
  });

  it('getAllTags fetches tags', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ id: 3 }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getAllTags } = await import('../src/api/tags');
    const res = await getAllTags();
    expect(getMock).toHaveBeenCalledWith('/tags');
    expect(res).toEqual([{ id: 3 }]);
  });
});
