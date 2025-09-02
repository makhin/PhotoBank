import { useAuthGetUser } from '../api/photobank';
import type { Claim, UserWithClaims } from '../types/claims';

export function useCanSeeNsfw(): boolean | null {
  const { data: userResp, isLoading, isError } = useAuthGetUser();
  if (isLoading) return null;
  if (isError) return false;
  const claims = (userResp?.data as UserWithClaims | undefined)?.claims ?? [];
  // поддержим несколько вариантов названий клейма
  return claims.some((c: Claim) => {
    const t = (c.type ?? '').toLowerCase();
    const v = (c.value ?? '').toLowerCase();
    return (t.includes('nsfw') || t.includes('canseensfw')) && (v === 'true' || v === '1');
  });
}
