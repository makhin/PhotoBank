import { getAllStoragesWithPaths } from '../dictionaries';
import { createDictionaryCommand } from './dictionaryFactory';

const MAX_PATHS_PER_STORAGE = 20;

export const storagesDictionary = createDictionaryCommand<{ name: string; paths: string[] }>({
  command: 'storages',
  fetchAll: () => Promise.resolve(getAllStoragesWithPaths()),
  errorKey: 'storages-error',
  mapItem: (storage) => {
    const paths = storage.paths.slice(0, MAX_PATHS_PER_STORAGE);
    const rest = storage.paths.length > MAX_PATHS_PER_STORAGE ? ['  ...'] : [];
    return {
      name: `${storage.name}\n${paths.map((path: string) => `  ${path}`).concat(rest).join('\n')}`,
    };
  },
});

export const sendStoragesPage = storagesDictionary.sendPage;
export const storagesCommand = storagesDictionary.commandHandler;
export const storagesCallbackPattern = storagesDictionary.callbackPattern;
export const registerStoragesDictionary = storagesDictionary.register;
