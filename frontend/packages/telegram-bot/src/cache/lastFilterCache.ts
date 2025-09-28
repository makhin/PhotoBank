import type { FilterDto } from '@photobank/shared/api/photobank';

export type LastFilterSource = 'search' | 'ai';

interface LastFilterEntry {
  filter: FilterDto;
  source: LastFilterSource;
}

const cache = new Map<number, LastFilterEntry>();

export function setLastFilter(
  chatId: number,
  filter: FilterDto,
  source: LastFilterSource,
): void {
  cache.set(chatId, { filter: { ...filter }, source });
}

export function getLastFilter(chatId: number): LastFilterEntry | undefined {
  const entry = cache.get(chatId);
  if (!entry) return undefined;

  return {
    filter: { ...entry.filter },
    source: entry.source,
  };
}

export function clearLastFilters(): void {
  cache.clear();
}
