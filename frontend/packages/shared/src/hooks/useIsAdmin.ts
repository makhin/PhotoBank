import * as AuthApi from '../api/photobank/auth/auth';

const ADMIN_ROLE = 'Administrator';
const ROLE_CLAIM_TYPES = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
];

export const useIsAdmin = (): boolean | null => {
  const { data: claims, isLoading, isError } = AuthApi.useAuthGetUserClaims();
  if (isLoading) return null;
  if (isError) return false;
  return (
    claims?.data.some(
      (c) => ROLE_CLAIM_TYPES.includes(c.type ?? '') && c.value === ADMIN_ROLE,
    ) ?? false
  );
};
