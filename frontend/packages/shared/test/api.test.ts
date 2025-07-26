import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('api helpers', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('searchPhotos posts filter', async () => {
    const postMock = vi.fn().mockResolvedValue({ count: 1 });
    vi.doMock('../src/generated', () => ({ PhotosService: { postApiPhotosSearch: postMock } }));
    const { searchPhotos } = await import('../src/api/photos');
    const res = await searchPhotos({ thisDay: true } as any);
    expect(postMock).toHaveBeenCalledWith({ thisDay: true } as any);
    expect(res).toEqual({ count: 1 });
  });

  it('searchPhotos returns cached result when available', async () => {
    const postMock = vi.fn().mockResolvedValue({ data: { count: 0 } });
    const cachedItem = { id: 1 };
    // simulate browser environment
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (global as any).window = { crypto: {} };
    vi.doMock('../src/generated', () => ({ PhotosService: { postApiPhotosSearch: postMock } }));
    vi.doMock('../src/cache/filterResultsCache', () => ({
      cacheFilterResult: vi.fn(),
      getCachedFilterResult: vi.fn().mockResolvedValue({ hash: 'h', count: 1, photos: [cachedItem] }),
    }));
    vi.doMock('../src/cache/photosCache', () => ({
      cachePhoto: vi.fn(),
      getCachedPhoto: vi.fn(),
    }));
    vi.doMock('../src/utils/getFilterHash', () => ({ getFilterHash: vi.fn().mockResolvedValue('h') }));
    const { searchPhotos } = await import('../src/api/photos');
    const res = await searchPhotos({} as any);
    expect(postMock).not.toHaveBeenCalled();
    expect(res).toEqual({ count: 1, photos: [cachedItem] });
  });

  it('getPhotoById requests by id', async () => {
    const getMock = vi.fn().mockResolvedValue({ id: 5 });
    vi.doMock('../src/generated', () => ({ PhotosService: { getApiPhotos: getMock } }));
    const { getPhotoById } = await import('../src/api/photos');
    const res = await getPhotoById(5);
    expect(getMock).toHaveBeenCalledWith(5);
    expect(res).toEqual({ id: 5 });
  });

  it('getAllPersons fetches persons', async () => {
    const getMock = vi.fn().mockResolvedValue([{ id: 1 }]);
    vi.doMock('../src/generated', () => ({ PersonsService: { getApiPersons: getMock } }));
    const { getAllPersons } = await import('../src/api/persons');
    const res = await getAllPersons();
    expect(getMock).toHaveBeenCalled();
    expect(res).toEqual([{ id: 1 }]);
  });

  it('getAllPaths fetches paths', async () => {
    const getMock = vi.fn().mockResolvedValue([{ storageId: 1 }]);
    vi.doMock('../src/generated', () => ({ PathsService: { getApiPaths: getMock } }));
    const { getAllPaths } = await import('../src/api/paths');
    const res = await getAllPaths();
    expect(getMock).toHaveBeenCalled();
    expect(res).toEqual([{ storageId: 1 }]);
  });

  it('getAllStorages fetches storages', async () => {
    const getMock = vi.fn().mockResolvedValue([{ id: 2 }]);
    vi.doMock('../src/generated', () => ({ StoragesService: { getApiStorages: getMock } }));
    const { getAllStorages } = await import('../src/api/storages');
    const res = await getAllStorages();
    expect(getMock).toHaveBeenCalled();
    expect(res).toEqual([{ id: 2 }]);
  });

  it('getAllTags fetches tags', async () => {
    const getMock = vi.fn().mockResolvedValue([{ id: 3 }]);
    vi.doMock('../src/generated', () => ({ TagsService: { getApiTags: getMock } }));
    const { getAllTags } = await import('../src/api/tags');
    const res = await getAllTags();
    expect(getMock).toHaveBeenCalled();
    expect(res).toEqual([{ id: 3 }]);
  });

  it('updateFace sends data', async () => {
    const putMock = vi.fn().mockResolvedValue({});
    vi.doMock('../src/generated', () => ({ FacesService: { putApiFaces: putMock } }));
    const { updateFace } = await import('../src/api/faces');
    await updateFace({ faceId: 5, personId: 2 });
    expect(putMock).toHaveBeenCalledWith({ faceId: 5, personId: 2 });
  });
});
