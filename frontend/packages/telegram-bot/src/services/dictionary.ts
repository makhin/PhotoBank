import type { Context } from 'grammy';

import { getPaths } from '../api/photobank/paths/paths';
import { getPersons } from '../api/photobank/persons/persons';
import { getStorages } from '../api/photobank/storages/storages';
import { getTags } from '../api/photobank/tags/tags';
import { callWithContext } from './call-with-context';

export async function fetchTags(ctx: Context) {
  const response = await callWithContext(ctx, () => getTags());
  return response.data ?? [];
}

export async function fetchPersons(ctx: Context) {
  const response = await callWithContext(ctx, () => getPersons());
  return response.data ?? [];
}

export async function fetchStorages(ctx: Context) {
  const response = await callWithContext(ctx, () => getStorages());
  return response.data ?? [];
}

export async function fetchPaths(ctx: Context) {
  const response = await callWithContext(ctx, () => getPaths());
  return response.data ?? [];
}
