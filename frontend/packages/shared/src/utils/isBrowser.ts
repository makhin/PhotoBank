export function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}
