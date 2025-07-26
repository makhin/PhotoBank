import type { UserWithClaimsDto, UpdateUserDto, ClaimDto } from '../generated';
import { UsersService } from '../generated';

export const getAllUsers = async (): Promise<UserWithClaimsDto[]> => {
  return UsersService.getApiAdminUsers();
};

export const updateUserById = async (
  id: string,
  data: UpdateUserDto,
): Promise<void> => {
  await UsersService.putApiAdminUsers(id, data);
};

export const setUserClaims = async (
  id: string,
  claims: ClaimDto[],
): Promise<void> => {
  await UsersService.putApiAdminUsersClaims(id, claims);
};
