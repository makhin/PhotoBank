import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  sendTagsPage,
  tagsCallbackPattern,
  tagsDictionary,
} from '../src/commands/tags';
import {
  sendPersonsPage,
  personsCallbackPattern,
  personsDictionary,
} from '../src/commands/persons';
import {
  sendStoragesPage,
  storagesCallbackPattern,
  storagesDictionary,
} from '../src/commands/storages';
import * as dict from '../src/dictionaries';
import { i18n } from '../src/i18n';

function createMockCtx() {
  return {
    reply: vi.fn().mockResolvedValue(undefined),
    editMessageText: vi.fn().mockResolvedValue(undefined),
    answerCallbackQuery: vi.fn().mockResolvedValue(undefined),
    t: (key: string, params?: Record<string, unknown>) => i18n.t('ru', key, params as any),
  } as any;
}

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('sendTagsPage', () => {
  it('filters by prefix and paginates', async () => {
    const tags = Array.from({ length: 11 }, (_, i) => ({ id: i + 1, name: `ba${String(i).padStart(2, '0')}` }));
    vi.spyOn(dict, 'getAllTags').mockReturnValue(tags as any);
    const ctx = createMockCtx();
    await sendTagsPage(ctx, 'ba', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('ba10');
    expect(text).toContain(i18n.t('ru', 'page-info', { page: 2, total: 2 }));
  });

  it('shows navigation to first and last pages', async () => {
    const tags = Array.from({ length: 25 }, (_, i) => ({ id: i + 1, name: `t${i}` }));
    vi.spyOn(dict, 'getAllTags').mockReturnValue(tags as any);
    const ctx = createMockCtx();
    await sendTagsPage(ctx, '', 2);
    const [, opts] = ctx.reply.mock.calls[0];
    const buttons = opts.reply_markup.inline_keyboard[0].map((b: any) => b.text);
    expect(buttons).toEqual([
      i18n.t('ru', 'first-page'),
      i18n.t('ru', 'prev-page'),
      i18n.t('ru', 'next-page'),
      i18n.t('ru', 'last-page'),
    ]);
  });
});

describe('sendPersonsPage', () => {
  it('filters by prefix and paginates', async () => {
    const persons = Array.from({ length: 12 }, (_, i) => ({ id: i + 1, name: `al${String(i).padStart(2, '0')}` }));
    vi.spyOn(dict, 'getAllPersons').mockReturnValue(persons as any);
    const ctx = createMockCtx();
    await sendPersonsPage(ctx, 'al', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('al10');
    expect(text).toContain(i18n.t('ru', 'page-info', { page: 2, total: 2 }));
  });

  it('skips persons with id below 1', async () => {
    const persons = [
      { id: 0, name: 'skip' },
      { id: 1, name: 'al00' },
    ];
    vi.spyOn(dict, 'getAllPersons').mockReturnValue(persons as any);
    const ctx = createMockCtx();
    await sendPersonsPage(ctx, 'a', 1);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).not.toContain('skip');
  });
});

describe('sendStoragesPage', () => {
  it('shows paths and paginates', async () => {
    const storages = Array.from({ length: 11 }, (_, i) => ({
      id: i + 1,
      name: `st${String(i).padStart(2, '0')}`,
      paths: [`p${i}`],
    }));
    vi.spyOn(dict, 'getAllStoragesWithPaths').mockReturnValue(storages as any);
    const ctx = createMockCtx();
    await sendStoragesPage(ctx, 'st', 2);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('st10');
    expect(text).toContain('p10');
    expect(text).toContain(i18n.t('ru', 'page-info', { page: 2, total: 2 }));
  });

  it('limits number of paths per storage', async () => {
    const storages = [
      {
        id: 1,
        name: 'st00',
        paths: Array.from({ length: 25 }, (_, i) => `p${i}`),
      },
    ];
    vi.spyOn(dict, 'getAllStoragesWithPaths').mockReturnValue(storages as any);
    const ctx = createMockCtx();
    await sendStoragesPage(ctx, '', 1);
    expect(ctx.reply).toHaveBeenCalled();
    const text = ctx.reply.mock.calls[0][0];
    expect(text).toContain('p19');
    expect(text).not.toContain('p20');
    expect(text).toContain('...');
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

  it('matches empty prefix for storages', () => {
    const match = 'storages:1:'.match(storagesCallbackPattern);
    expect(match?.[1]).toBe('1');
    expect(match?.[2]).toBe('');
  });
});

describe('dictionary registration', () => {
  async function expectDictionaryRegistration(dictionary: typeof tagsDictionary) {
    const bot = {
      command: vi.fn(),
      callbackQuery: vi.fn(),
    } as any;
    const withRegistered = vi.fn((handler) => handler as any);
    const sendSpy = vi.spyOn(dictionary, 'sendPage').mockResolvedValue(undefined);

    dictionary.register(bot, withRegistered);

    expect(bot.command).toHaveBeenCalledWith(dictionary.command, expect.any(Function));
    expect(bot.callbackQuery).toHaveBeenCalledWith(dictionary.callbackPattern, expect.any(Function));
    expect(withRegistered).toHaveBeenCalledTimes(2);

    const commandHandler = bot.command.mock.calls[0][1];
    const ctx = Object.assign(createMockCtx(), {
      message: { text: `/${dictionary.command} prefix` },
    });
    await commandHandler(ctx);
    expect(sendSpy).toHaveBeenCalledWith(ctx, 'prefix', 1);

    const callbackHandler = bot.callbackQuery.mock.calls[0][1];
    const match = ['full', '2', 'pref%20ix'] as RegExpExecArray;
    match.index = 0;
    match.input = `${dictionary.command}:2:pref%20ix`;
    const callbackCtx = Object.assign(createMockCtx(), {
      match,
      answerCallbackQuery: vi.fn().mockResolvedValue(undefined),
    });
    await callbackHandler(callbackCtx);
    expect(callbackCtx.answerCallbackQuery).toHaveBeenCalled();
    expect(sendSpy).toHaveBeenLastCalledWith(callbackCtx, 'pref ix', 2, true);
    sendSpy.mockRestore();
  }

  it('registers tags handlers', async () => {
    await expectDictionaryRegistration(tagsDictionary);
  });

  it('registers persons handlers', async () => {
    await expectDictionaryRegistration(personsDictionary);
  });

  it('registers storages handlers', async () => {
    await expectDictionaryRegistration(storagesDictionary);
  });
});
