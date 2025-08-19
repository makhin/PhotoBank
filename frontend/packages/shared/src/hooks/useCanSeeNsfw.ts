import { useAuthGetUserClaims } from '../api/photobank';
export function useCanSeeNsfw(): boolean | null {
  const { data, isLoading, isError } = useAuthGetUserClaims();
  if (isLoading) return null;
  if (isError) return false;
  const claims = data?.data ?? [];
  // поддержим несколько вариантов названий клейма
  return claims.some(c => {
    const t = (c.type ?? '').toLowerCase();
    const v = (c.value ?? '').toLowerCase();
    return (t.includes('nsfw') || t.includes('canseensfw')) && (v === 'true' || v === '1');
  });
}
