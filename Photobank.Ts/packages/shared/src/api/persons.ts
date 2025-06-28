import { PersonDto } from '../types';
import { apiClient } from './client';

export const getAllPersons = async (): Promise<PersonDto[]> => {
  const response = await apiClient.get('/persons');
  return response.data;
};
