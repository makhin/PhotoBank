import { useAuthGetUser } from '../api/photobank';

export function useCanSeeNsfw(): boolean | null {
  const { data: userResp, isLoading, isError } = useAuthGetUser();
  if (isLoading) return null;
  if (isError) return false;
  const claims = (userResp?.data as any)?.claims ?? [];
  // поддержим несколько вариантов названий клейма
  return claims.some((c: { type?: string | null; value?: string | null }) => {
    const t = (c.type ?? '').toLowerCase();
    const v = (c.value ?? '').toLowerCase();
    return (t.includes('nsfw') || t.includes('canseensfw')) && (v === 'true' || v === '1');
  });
}
