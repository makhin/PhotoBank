import { describe, expect, it } from 'vitest';

import type { PersonDto, StorageDto, TagDto } from '../src/api/photobank/photoBankApi.schemas';
import {
  buildPersonMap,
  buildPersonSearchIndex,
  buildStorageMap,
  buildStorageSearchIndex,
  buildTagMap,
  buildTagSearchIndex,
} from '../src/metadata';

describe('metadata builders', () => {
  const tags: TagDto[] = [
    { id: 1, name: 'Beach' },
    { id: 2, name: 'Mountains' },
  ];

  const persons: PersonDto[] = [
    { id: 1, name: 'Anna' },
    { id: 2, name: 'Ben' },
  ];

  const storages: StorageDto[] = [
    { id: 4, name: 'Archive' },
    { id: 9, name: 'Family Library' },
  ];

  it('builds memoised tag maps', () => {
    const map = buildTagMap(tags);

    expect(map.get(1)?.name).toBe('Beach');
    expect(buildTagMap(tags)).toBe(map);

    const otherReference = buildTagMap(tags.slice());

    expect(otherReference).not.toBe(map);
    expect(otherReference.get(2)?.name).toBe('Mountains');
  });

  it('builds memoised person maps', () => {
    const map = buildPersonMap(persons);

    expect(map.get(2)?.name).toBe('Ben');
    expect(buildPersonMap(persons)).toBe(map);

    expect(buildPersonMap(undefined).size).toBe(0);
  });

  it('builds memoised storage maps', () => {
    const map = buildStorageMap(storages);

    expect(map.get(9)?.name).toBe('Family Library');
    expect(buildStorageMap(storages)).toBe(map);
  });

  it('creates fuzzy indexes for tags', () => {
    const index = buildTagSearchIndex(tags);

    expect(buildTagSearchIndex(tags)).toBe(index);
    expect(index.search('beach')[0]?.item).toMatchObject({ id: 1 });
  });

  it('creates fuzzy indexes for persons', () => {
    const index = buildPersonSearchIndex(persons);

    const [match] = index.search('anna');

    expect(match?.item.id).toBe(1);
  });

  it('creates fuzzy indexes for storages', () => {
    const index = buildStorageSearchIndex(storages);

    expect(index.search('Family')[0]?.item.id).toBe(9);
    expect(buildStorageSearchIndex(undefined).search('anything')).toHaveLength(0);
  });
});

