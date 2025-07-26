import type { PersonDto } from '../generated';
import { PersonsService } from '../generated';

export const getAllPersons = async (): Promise<PersonDto[]> => {
  return PersonsService.getApiPersons();
};
