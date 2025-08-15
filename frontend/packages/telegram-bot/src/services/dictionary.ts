import type { Context } from 'grammy';

import { getPaths } from '../api/photobank/paths/paths';
import { getPersons } from '../api/photobank/persons/persons';
import { getStorages } from '../api/photobank/storages/storages';
import { getTags } from '../api/photobank/tags/tags';
import { handleServiceError } from '../errorHandler';

const { pathsGetAll } = getPaths();
const { personsGetAll } = getPersons();
const { storagesGetAll } = getStorages();
const { tagsGetAll } = getTags();

export async function fetchTags(ctx: Context) {
  try {
    return await tagsGetAll(ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPersons(ctx: Context) {
  try {
    return await personsGetAll(ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchStorages(ctx: Context) {
  try {
    return await storagesGetAll(ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPaths(ctx: Context) {
  try {
    return await pathsGetAll(ctx);
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
