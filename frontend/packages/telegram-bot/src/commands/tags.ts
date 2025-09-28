import { getAllTags } from '../dictionaries';
import { createDictionaryCommand } from './dictionaryFactory';

export const tagsDictionary = createDictionaryCommand({
  command: 'tags',
  fetchAll: () => Promise.resolve(getAllTags()),
  errorKey: 'tags-error',
});

export const sendTagsPage = tagsDictionary.sendPage;
export const tagsCommand = tagsDictionary.commandHandler;
export const tagsCallbackPattern = tagsDictionary.callbackPattern;
export const registerTagsDictionary = tagsDictionary.register;
