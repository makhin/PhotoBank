import {getAllPersons} from "@photobank/shared/api";

let tagMap = new Map<number, string>();
let personMap = new Map<number, string>();
let storageMap = new Map<number, string>();
let pathMap = new Map<number, string>();

export async function loadDictionaries() {
//    tagMap = new Map((await getAllTags()).map(tag => [tag.id, tag.name]));
    personMap = new Map((await getAllPersons()).map(p => [p.id, p.name]));
//    storageMap = new Map((await getAllStorages()).map(p => [p.id, p.name]));
//    pathMap = new Map((await getAllPaths()).map(p => [p.storageId, p.path]));
}

export function getTagName(id: number): string {
    return tagMap.get(id) ?? `#${id}`;
}

export function getPersonName(id: number): string {
    return personMap.get(id) ?? `ID ${id}`;
}

export function getStorageName(id: number): string {
    return storageMap.get(id) ?? `ID ${id}`;
}
