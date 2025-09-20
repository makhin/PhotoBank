import type { Context } from 'grammy';

import { getPaths } from '../api/photobank/paths/paths';
import { getPersons } from '../api/photobank/persons/persons';
import { getStorages } from '../api/photobank/storages/storages';
import { getTags } from '../api/photobank/tags/tags';
import { callWithContext } from './call-with-context';

const { pathsGetAll } = getPaths();
const { personsGetAll } = getPersons();
const { storagesGetAll } = getStorages();
const { tagsGetAll } = getTags();

export async function fetchTags(ctx: Context) {
  return callWithContext(ctx, tagsGetAll);
}

export async function fetchPersons(ctx: Context) {
  return callWithContext(ctx, personsGetAll);
}

export async function fetchStorages(ctx: Context) {
  return callWithContext(ctx, storagesGetAll);
}

export async function fetchPaths(ctx: Context) {
  return callWithContext(ctx, pathsGetAll);
}
