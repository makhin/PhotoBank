import Fuse from 'fuse.js';

import type { PersonDto, StorageDto, TagDto } from '../api/photobank/photoBankApi.schemas';

type EntityWithId = { id: number };
type EntityWithName = { name: string };

type MaybeEntities<TEntity> = readonly TEntity[] | null | undefined;

const createMemoizedBuilder = <TEntity, TResult>(
  factory: (entities: readonly TEntity[]) => TResult,
) => {
  const cache = new WeakMap<object, TResult>();
  let emptyCache: TResult | undefined;

  return (entities?: MaybeEntities<TEntity>): TResult => {
    if (!entities || entities.length === 0) {
      if (!emptyCache) {
        emptyCache = factory([]);
      }

      return emptyCache;
    }

    const key = entities as unknown as object;

    if (cache.has(key)) {
      return cache.get(key)!;
    }

    const result = factory(entities);
    cache.set(key, result);

    return result;
  };
};

const createMapBuilder = <TEntity extends EntityWithId>() =>
  createMemoizedBuilder<TEntity, ReadonlyMap<number, TEntity>>((entities) =>
    new Map(entities.map((entity) => [entity.id, entity] as const)),
  );

const createFuzzyIndexBuilder = <TEntity extends EntityWithName>() => {
  const options: Fuse.IFuseOptions<TEntity> = {
    keys: ['name'],
    threshold: 0.3,
    ignoreLocation: true,
  };

  return createMemoizedBuilder<TEntity, Fuse<TEntity>>(
    (entities) => new Fuse(Array.from(entities), options),
  );
};

export type TagMap = ReadonlyMap<number, TagDto>;
export type PersonMap = ReadonlyMap<number, PersonDto>;
export type StorageMap = ReadonlyMap<number, StorageDto>;

export const buildTagMap = createMapBuilder<TagDto>();
export const buildPersonMap = createMapBuilder<PersonDto>();
export const buildStorageMap = createMapBuilder<StorageDto>();

export type TagSearchIndex = Fuse<TagDto>;
export type PersonSearchIndex = Fuse<PersonDto>;
export type StorageSearchIndex = Fuse<StorageDto>;

export const buildTagSearchIndex = createFuzzyIndexBuilder<TagDto>();
export const buildPersonSearchIndex = createFuzzyIndexBuilder<PersonDto>();
export const buildStorageSearchIndex = createFuzzyIndexBuilder<StorageDto>();

