import { pathsGetAll, personsGetAll, storagesGetAll, tagsGetAll } from '@photobank/shared/api/photobank';

import { handleServiceError } from '../errorHandler';

export async function fetchTags() {
  try {
    return await tagsGetAll();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPersons() {
  try {
    return await personsGetAll();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchStorages() {
  try {
    return await storagesGetAll();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function fetchPaths() {
  try {
    return await pathsGetAll();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
