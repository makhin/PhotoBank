import type { Context } from 'grammy';

import { getPaths } from '../api/photobank/paths/paths';
import { getPersons } from '../api/photobank/persons/persons';
import { getStorages } from '../api/photobank/storages/storages';
import { getTags } from '../api/photobank/tags/tags';
import { callWithContext } from './call-with-context';

const { getPaths: fetchPathsRequest } = getPaths();
const { personsGetAll: fetchPersonsRequest } = getPersons();
const { getStorages: fetchStoragesRequest } = getStorages();
const { getTags: fetchTagsRequest } = getTags();

export async function fetchTags(ctx: Context) {
  return callWithContext(ctx, fetchTagsRequest);
}

export async function fetchPersons(ctx: Context) {
  return callWithContext(ctx, fetchPersonsRequest);
}

export async function fetchStorages(ctx: Context) {
  return callWithContext(ctx, fetchStoragesRequest);
}

export async function fetchPaths(ctx: Context) {
  return callWithContext(ctx, fetchPathsRequest);
}
