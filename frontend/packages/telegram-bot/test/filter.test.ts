import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FilterDto } from '@photobank/shared/api/photobank';
import type { MyContext } from '../src/i18n';
import { filterCommand } from '../src/commands/filter';

const getLastFilter = vi.fn();

vi.mock('../src/cache/lastFilterCache', () => ({
  getLastFilter: (...args: unknown[]) => getLastFilter(...args),
}));

describe('filterCommand', () => {
  beforeEach(() => {
    getLastFilter.mockReset();
  });

  it('replies with error message when chat is missing', async () => {
    const reply = vi.fn(() => Promise.resolve());
    const t = vi.fn((key: string) => key);
    const ctx = {
      reply,
      t,
    } as unknown as MyContext;

    await filterCommand(ctx);

    expect(t).toHaveBeenCalledWith('chat-undetermined');
    expect(reply).toHaveBeenCalledWith('chat-undetermined');
    expect(getLastFilter).not.toHaveBeenCalled();
  });

  it('returns empty payload when filter history is missing', async () => {
    const reply = vi.fn(() => Promise.resolve());
    const t = vi.fn((key: string) => {
      if (key === 'filter-empty') return 'filter-empty\n{}';
      return key;
    });
    const ctx = {
      chat: { id: 7 },
      reply,
      t,
    } as unknown as MyContext;

    getLastFilter.mockReturnValue(undefined);

    await filterCommand(ctx);

    expect(getLastFilter).toHaveBeenCalledWith(7);
    expect(reply).toHaveBeenCalledWith('filter-empty\n{}');
  });

  it('prints saved filter with ISO dates', async () => {
    const reply = vi.fn(() => Promise.resolve());
    const t = vi.fn((key: string, params?: Record<string, string>) => {
      if (key === 'filter-source-ai') return 'AI command';
      if (key === 'filter-source-search') return 'Search command';
      if (key === 'filter-last') {
        return `last:${params?.source}:${params?.filter}`;
      }
      return key;
    });
    const ctx = {
      chat: { id: 11 },
      reply,
      t,
    } as unknown as MyContext;

    const filter: FilterDto = {
      caption: 'cats',
      takenDateFrom: new Date('2024-05-01T00:00:00Z'),
      page: 3,
      pageSize: 10,
    };

    getLastFilter.mockReturnValue({
      source: 'ai',
      filter,
    });

    await filterCommand(ctx);

    const expectedJson = JSON.stringify(
      filter,
      (_key, value) => (value instanceof Date ? value.toISOString() : value),
      2,
    );

    expect(reply).toHaveBeenCalledWith(`last:AI command:${expectedJson}`);
  });
});
