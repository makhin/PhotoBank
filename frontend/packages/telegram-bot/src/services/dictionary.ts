import type { Context } from 'grammy';

import { getPaths } from '../api/photobank/paths/paths';
import { getPersons } from '../api/photobank/persons/persons';
import { getStorages } from '../api/photobank/storages/storages';
import { getTags } from '../api/photobank/tags/tags';
import { setRequestContext } from '../api/axios-instance';
import { handleServiceError } from '../errorHandler';

const { pathsGetAll } = getPaths();
const { personsGetAll } = getPersons();
const { storagesGetAll } = getStorages();
const { tagsGetAll } = getTags();

export async function fetchTags(ctx: Context) {
  try {
    setRequestContext(ctx);
    return await tagsGetAll();
  } catch (err: unknown) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPersons(ctx: Context) {
  try {
    setRequestContext(ctx);
    return await personsGetAll();
  } catch (err: unknown) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchStorages(ctx: Context) {
  try {
    setRequestContext(ctx);
    return await storagesGetAll();
  } catch (err: unknown) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPaths(ctx: Context) {
  try {
    setRequestContext(ctx);
    return await pathsGetAll();
  } catch (err: unknown) {
    handleServiceError(err);
    throw err;
  }
}
