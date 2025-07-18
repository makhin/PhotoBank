import type { UserWithClaimsDto, UpdateUserDto, ClaimDto } from '../types';
import { apiClient } from './client';

export const getAllUsers = async (): Promise<UserWithClaimsDto[]> => {
  const res = await apiClient.get<UserWithClaimsDto[]>('/admin/users');
  return res.data;
};

export const updateUserById = async (
  id: string,
  data: UpdateUserDto,
): Promise<void> => {
  await apiClient.put(`/admin/users/${id}`, data);
};

export const setUserClaims = async (
  id: string,
  claims: ClaimDto[],
): Promise<void> => {
  await apiClient.put(`/admin/users/${id}/claims`, claims);
};
