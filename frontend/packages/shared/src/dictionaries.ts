import { PersonsService, TagsService } from '@photobank/shared/generated';
import { unknownPersonLabel } from '@photobank/shared/constants';
import type { PersonDto, TagDto } from '@photobank/shared/generated';

let tagMap = new Map<number, string>();
let personMap = new Map<number, string>();
let tagList: TagDto[] = [];
let personList: PersonDto[] = [];
let storageMap = new Map<number, string>();
let pathMap = new Map<number, string>();

export async function loadDictionaries() {
    tagList = await TagsService.getApiTags();
    tagMap = new Map(tagList.map(tag => [tag.id, tag.name]));
    personList = await PersonsService.getApiPersons();
    personMap = new Map(personList.map(p => [p.id, p.name]));
//    storageMap = new Map((await getAllStorages()).map(p => [p.id, p.name]));
//    pathMap = new Map((await getAllPaths()).map(p => [p.storageId, p.path]));
}

export function getTagName(id: number): string {
    return tagMap.get(id) ?? `#${id}`;
}

export function getAllTags(): TagDto[] {
    return tagList;
}

export function getPersonName(id: number | null | undefined): string {
    if (id === null || id === undefined) return unknownPersonLabel;
    return personMap.get(id) ?? `ID ${id}`;
}

export function getAllPersons(): PersonDto[] {
    return personList;
}

export function getStorageName(id: number): string {
    return storageMap.get(id) ?? `ID ${id}`;
}

function similarity(a: string, b: string): number {
    const dp: number[][] = Array.from({ length: a.length + 1 }, () => new Array(b.length + 1).fill(0));
    for (let i = 0; i <= a.length; i++) dp[i][0] = i;
    for (let j = 0; j <= b.length; j++) dp[0][j] = j;
    for (let i = 1; i <= a.length; i++) {
        for (let j = 1; j <= b.length; j++) {
            dp[i][j] = Math.min(
                dp[i - 1][j] + 1,
                dp[i][j - 1] + 1,
                dp[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1)
            );
        }
    }
    const dist = dp[a.length][b.length];
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
    return findBestId(personMap, name);
}

export function findBestTagId(name: string): number | undefined {
    return findBestId(tagMap, name);
}
