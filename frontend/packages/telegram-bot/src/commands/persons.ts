import { getAllPersons } from '../dictionaries';
import { createDictionaryCommand } from './dictionaryFactory';

export const personsDictionary = createDictionaryCommand({
  command: 'persons',
  fetchAll: () => Promise.resolve(getAllPersons()),
  errorKey: 'persons-error',
  filter: (person) => person.id >= 1,
});

export const sendPersonsPage = personsDictionary.sendPage;
export const personsCommand = personsDictionary.commandHandler;
export const personsCallbackPattern = personsDictionary.callbackPattern;
export const registerPersonsDictionary = personsDictionary.register;
