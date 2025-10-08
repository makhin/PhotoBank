import type { Context } from 'grammy';
import { getPaths, getStorages, getTags, personsGetAll } from '@photobank/shared';

import { callWithContext } from './call-with-context';

export async function fetchTags(ctx: Context) {
  const response = await callWithContext(ctx, () => getTags());
  return response.data ?? [];
}

export async function fetchPersons(ctx: Context) {
  const response = await callWithContext(ctx, () => personsGetAll());
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
