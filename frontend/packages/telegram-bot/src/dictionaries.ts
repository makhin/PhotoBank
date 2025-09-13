import type {
  PersonDto,
  StorageDto,
  TagDto,
} from '@photobank/shared/api/photobank';

import { i18n } from './i18n';
import type { MyContext } from './i18n';
import {
  fetchPaths,
  fetchPersons,
  fetchStorages,
  fetchTags,
} from './services/dictionary';

type DictData = {
  tagMap: Map<number, string>;
  personMap: Map<number, string>;
  tagList: TagDto[];
  personList: PersonDto[];
  storageList: StorageDto[];
  storageMap: Map<number, string>;
  pathsMap: Map<number, string[]>;
};

const cache = new Map<string, DictData>();
let currentUser = '';
let currentLocale = 'en';

export function setDictionariesUser(userId: number | string | null | undefined, locale?: string) {
  currentUser = String(userId ?? '');
  if (locale) currentLocale = locale;
}

function getDict(): DictData {
  const existing = cache.get(currentUser);
  if (existing) return existing;
  return {
    tagMap: new Map<number, string>(),
    personMap: new Map<number, string>(),
    tagList: [],
    personList: [],
    storageList: [],
    storageMap: new Map<number, string>(),
    pathsMap: new Map<number, string[]>(),
  };
}

export async function loadDictionaries(ctx: MyContext) {
  if (cache.has(currentUser)) return;
  const tagList = await fetchTags(ctx);
  const tagMap = new Map<number, string>(tagList.map((t) => [t.id, t.name]));
  const personList = await fetchPersons(ctx);
  const personMap = new Map<number, string>(personList.map((p) => [p.id, p.name]));
  const storageList = await fetchStorages(ctx);
  const storageMap = new Map<number, string>(storageList.map((s) => [s.id, s.name]));
  const pathList = await fetchPaths(ctx);
  const pathsMap = new Map<number, string[]>();
  for (const p of pathList) {
    const arr = pathsMap.get(p.storageId) ?? [];
    arr.push(p.path);
    pathsMap.set(p.storageId, arr);
  }
  cache.set(currentUser, {
    tagMap,
    personMap,
    tagList,
    personList,
    storageList,
    storageMap,
    pathsMap,
  });
}

export function getTagName(id: number): string {
  return getDict().tagMap.get(id) ?? `#${String(id)}`;
}

export function getAllTags(): TagDto[] {
  return getDict().tagList;
}

export function getPersonName(id: number | null | undefined): string {
  if (id === null || id === undefined) return i18n.t(currentLocale, 'unknown-person');
  return getDict().personMap.get(id) ?? `ID ${String(id)}`;
}

export function getAllPersons(): PersonDto[] {
  return getDict().personList;
}

export function getStorageName(id: number): string {
  return getDict().storageMap.get(id) ?? `ID ${String(id)}`;
}

export function getStorageId(name: string): number {
  const storage = Array.from(getDict().storageMap.entries()).find(
    ([, value]) => value === name
  );

  if (!storage) {
    throw new Error(`Storage with name "${name}" not found`);
  }

  return storage[0];
}

export function getAllStoragesWithPaths(): Array<StorageDto & { paths: string[] }> {
  const dict = getDict();
  return dict.storageList.map((s) => ({ ...s, paths: dict.pathsMap.get(s.id) ?? [] }));
}

function similarity(a: string, b: string): number {
  const dp: number[][] = Array.from({ length: a.length + 1 }, () =>
    Array<number>(b.length + 1).fill(0)
  );
  for (let i = 0; i <= a.length; i++) dp[i]![0] = i;
  for (let j = 0; j <= b.length; j++) dp[0]![j] = j;
  for (let i = 1; i <= a.length; i++) {
    for (let j = 1; j <= b.length; j++) {
      dp[i]![j] = Math.min(
        dp[i - 1]![j]! + 1,
        dp[i]![j - 1]! + 1,
        dp[i - 1]![j - 1]! + (a[i - 1] === b[j - 1] ? 0 : 1)
      );
    }
  }
  const dist = dp[a.length]![b.length]!;
  const maxLen = Math.max(a.length, b.length) || 1;
  return (maxLen - dist) / maxLen;
}

function findBestId(map: Map<number, string>, name: string): number | undefined {
  const lower = name.toLowerCase();
  let bestId: number | undefined;
  let bestScore = 0;
  for (const [id, value] of map.entries()) {
    const score = similarity(lower, value.toLowerCase());
    if (score > bestScore) {
      bestScore = score;
      bestId = id;
    }
  }
  return bestScore >= 0.5 ? bestId : undefined;
}

export function findBestPersonId(name: string): number | undefined {
  return findBestId(getDict().personMap, name);
}

export function findBestTagId(name: string): number | undefined {
  return findBestId(getDict().tagMap, name);
}