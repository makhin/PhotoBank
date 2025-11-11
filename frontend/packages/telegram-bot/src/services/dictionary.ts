import type { Context } from 'grammy';
import { getPaths, getStorages, getTags, personsGetAll } from '@photobank/shared';

import { callWithContext } from './call-with-context';
import { logger } from '../logger';

export async function fetchTags(ctx: Context) {
  const response = await callWithContext(ctx, () => getTags());
  if (!Array.isArray(response.data)) {
    logger.error('fetchTags received non-array data:', response.data);
    return [];
  }
  return response.data ?? [];
}

export async function fetchPersons(ctx: Context) {
  const response = await callWithContext(ctx, () => personsGetAll());
  if (!Array.isArray(response.data)) {
    logger.error('fetchPersons received non-array data:', response.data);
    return [];
  }
  return response.data ?? [];
}

export async function fetchStorages(ctx: Context) {
  const response = await callWithContext(ctx, () => getStorages());
  if (!Array.isArray(response.data)) {
    logger.error('fetchStorages received non-array data:', response.data);
    return [];
  }
  return response.data ?? [];
}

export async function fetchPaths(ctx: Context) {
  const response = await callWithContext(ctx, () => getPaths());
  if (!Array.isArray(response.data)) {
    logger.error('fetchPaths received non-array data:', response.data);
    return [];
  }
  return response.data ?? [];
}
