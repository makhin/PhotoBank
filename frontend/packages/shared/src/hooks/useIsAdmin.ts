import * as AuthApi from '../api/photobank/auth/auth';

export const useIsAdmin = (): boolean | null => {
  const { data: roles, isLoading, isError } = AuthApi.useAuthGetUserRoles();
  if (isLoading) return null;
  if (isError) return false;
  return roles?.data.some((r) => r.name === 'Administrator') ?? false;
};
