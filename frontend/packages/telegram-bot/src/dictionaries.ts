import type {
  PersonDto,
  StorageDto,
  TagDto,
} from '@photobank/shared/api/photobank';
import {
  buildPersonMap,
  buildPersonSearchIndex,
  buildStorageMap,
  buildTagMap,
  buildTagSearchIndex,
  type PersonMap,
  type PersonSearchIndex,
  type StorageMap,
  type TagMap,
  type TagSearchIndex,
} from '@photobank/shared/metadata';

import { i18n } from './i18n';
import type { MyContext } from './i18n';
import {
  fetchPaths,
  fetchPersons,
  fetchStorages,
  fetchTags,
} from './services/dictionary';

type DictData = {
  tagMap: TagMap;
  personMap: PersonMap;
  storageMap: StorageMap;
  tagIndex: TagSearchIndex;
  personIndex: PersonSearchIndex;
  tagList: TagDto[];
  personList: PersonDto[];
  storageList: StorageDto[];
  pathsMap: Map<number, string[]>;
};

const cache = new Map<string, DictData>();
let currentUser = '';
let currentLocale = 'en';

export function setDictionariesUser(userId: number | string | null | undefined, locale?: string) {
  currentUser = String(userId ?? '');
  if (locale) currentLocale = locale;
}

export function getCurrentLocale(): string {
  return currentLocale;
}

function createEmptyDict(): DictData {
  return {
    tagMap: buildTagMap(undefined),
    personMap: buildPersonMap(undefined),
    storageMap: buildStorageMap(undefined),
    tagIndex: buildTagSearchIndex(undefined),
    personIndex: buildPersonSearchIndex(undefined),
    tagList: [],
    personList: [],
    storageList: [],
    pathsMap: new Map<number, string[]>(),
  };
}

function getDict(): DictData {
  const existing = cache.get(currentUser);
  if (existing) return existing;
  return createEmptyDict();
}

export async function loadDictionaries(ctx: MyContext) {
  if (cache.has(currentUser)) return;
  const tagList = await fetchTags(ctx);
  const personList = await fetchPersons(ctx);
  const storageList = await fetchStorages(ctx);
  const pathList = await fetchPaths(ctx);
  const tagMap = buildTagMap(tagList);
  const personMap = buildPersonMap(personList);
  const storageMap = buildStorageMap(storageList);
  const tagIndex = buildTagSearchIndex(tagList);
  const personIndex = buildPersonSearchIndex(personList);
  const pathsMap = new Map<number, string[]>();
  for (const p of pathList) {
    const arr = pathsMap.get(p.storageId) ?? [];
    arr.push(p.path);
    pathsMap.set(p.storageId, arr);
  }
  cache.set(currentUser, {
    tagMap,
    personMap,
    tagIndex,
    personIndex,
    tagList,
    personList,
    storageList,
    storageMap,
    pathsMap,
  });
}

export function getTagName(id: number): string {
  return getDict().tagMap.get(id)?.name ?? `#${String(id)}`;
}

export function getAllTags(): TagDto[] {
  return getDict().tagList;
}

export function getPersonName(id: number | null | undefined): string {
  if (id === null || id === undefined) return i18n.t(currentLocale, 'unknown-person');
  return getDict().personMap.get(id)?.name ?? `ID ${String(id)}`;
}

export function getAllPersons(): PersonDto[] {
  return getDict().personList;
}

export function getStorageName(id: number): string {
  return getDict().storageMap.get(id)?.name ?? `ID ${String(id)}`;
}

export function getStorageId(name: string): number {
  const storage = getDict().storageList.find((value) => value.name === name);

  if (!storage) {
    throw new Error(`Storage with name "${name}" not found`);
  }

  return storage.id;
}

export function getAllStoragesWithPaths(): Array<StorageDto & { paths: string[] }> {
  const dict = getDict();
  return dict.storageList.map((s) => ({ ...s, paths: dict.pathsMap.get(s.id) ?? [] }));
}

export function findBestPersonId(name: string): number | undefined {
  const [match] = getDict().personIndex.search(name);
  return match?.item.id;
}

export function findBestTagId(name: string): number | undefined {
  const [match] = getDict().tagIndex.search(name);
  return match?.item.id;
}
