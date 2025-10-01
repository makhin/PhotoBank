import { useAuthGetUser } from '../api/photobank';
import type { Claim, UserWithClaims } from '../types/claims';

const ADMIN_ROLE = 'Admin';
const ROLE_CLAIM_TYPES = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
];

export const hasAdminRole = (
  roles?: readonly string[] | null,
): boolean => roles?.includes(ADMIN_ROLE) ?? false;

export const useIsAdmin = (): boolean | null => {
  const { data: userResp, isLoading, isError } = useAuthGetUser();
  if (isLoading) return null;
  if (isError) return false;

  if (hasAdminRole(userResp?.data?.roles)) {
    return true;
  }

  const claims = (userResp?.data as UserWithClaims | undefined)?.claims ?? [];
  return claims.some(
    (c: Claim) =>
      ROLE_CLAIM_TYPES.includes(c.type ?? '') && c.value === ADMIN_ROLE,
  );
};
