import { describe, it, expect, vi } from 'vitest';
import { sendTagsPage } from '../src/commands/tags';
import { sendPersonsPage } from '../src/commands/persons';
import * as api from '@photobank/shared/api';

describe('sendTagsPage', () => {
  it('filters by prefix and paginates', async () => {
    const tags = Array.from({ length: 11 }, (_, i) => ({ id: i + 1, name: `ba${String(i).padStart(2, '0')}` }));
    vi.spyOn(api, 'getAllTags').mockResolvedValue(tags as any);
    const ctx = { reply: vi.fn() } as any;
    await sendTagsPage(ctx, 'ba', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('ba10');
    expect(text).toContain('Страница 2 из 2');
  });
});

describe('sendPersonsPage', () => {
  it('filters by prefix and paginates', async () => {
    const persons = Array.from({ length: 12 }, (_, i) => ({ id: i + 1, name: `al${String(i).padStart(2, '0')}` }));
    vi.spyOn(api, 'getAllPersons').mockResolvedValue(persons as any);
    const ctx = { reply: vi.fn() } as any;
    await sendPersonsPage(ctx, 'al', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('al10');
    expect(text).toContain('Страница 2 из 2');
  });
});
