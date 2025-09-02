import { useAuthGetUser } from '../api/photobank';

const ADMIN_ROLE = 'Administrator';
const ROLE_CLAIM_TYPES = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
];

export const useIsAdmin = (): boolean | null => {
  const { data: userResp, isLoading, isError } = useAuthGetUser();
  if (isLoading) return null;
  if (isError) return false;
  const claims = (userResp?.data as any)?.claims ?? [];
  return claims.some(
    (c: { type?: string | null; value?: string | null }) =>
      ROLE_CLAIM_TYPES.includes(c.type ?? '') && c.value === ADMIN_ROLE,
  );
};
