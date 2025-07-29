import { describe, it, expect, vi } from 'vitest';
import { sendTagsPage } from '../src/commands/tags';
import { sendPersonsPage } from '../src/commands/persons';
import { tagsCallbackPattern, personsCallbackPattern } from '../src/patterns';
import * as dict from '@photobank/shared/dictionaries';

describe('sendTagsPage', () => {
  it('filters by prefix and paginates', async () => {
    const tags = Array.from({ length: 11 }, (_, i) => ({ id: i + 1, name: `ba${String(i).padStart(2, '0')}` }));
    vi.spyOn(dict, 'getAllTags').mockReturnValue(tags as any);
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
    vi.spyOn(dict, 'getAllPersons').mockReturnValue(persons as any);
    const ctx = { reply: vi.fn() } as any;
    await sendPersonsPage(ctx, 'al', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('al10');
    expect(text).toContain('Страница 2 из 2');
  });

  it('skips persons with id below 1', async () => {
    const persons = [
      { id: 0, name: 'skip' },
      { id: 1, name: 'al00' },
    ];
    vi.spyOn(dict, 'getAllPersons').mockReturnValue(persons as any);
    const ctx = { reply: vi.fn() } as any;
    await sendPersonsPage(ctx, 'a', 1);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).not.toContain('skip');
  });
});

describe('callback regex', () => {
  it('matches empty prefix for tags', () => {
    const match = 'tags:2:'.match(tagsCallbackPattern);
    expect(match?.[1]).toBe('2');
    expect(match?.[2]).toBe('');
  });

  it('matches empty prefix for persons', () => {
    const match = 'persons:3:'.match(personsCallbackPattern);
    expect(match?.[1]).toBe('3');
    expect(match?.[2]).toBe('');
  });
});
