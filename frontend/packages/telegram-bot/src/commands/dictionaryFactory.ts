import type { Bot, MiddlewareFn } from 'grammy';

import type { MyContext } from '../i18n';
import { parsePrefix, sendNamedItemsPage, type NamedItem } from './helpers';

export type WithRegistered = <T extends MyContext>(
  handler: (ctx: T) => Promise<void>,
) => MiddlewareFn<T>;

export interface DictionaryFactoryOptions<T extends { name: string }> {
  command: string;
  fetchAll: () => Promise<T[]> | T[];
  errorKey: string;
  mapItem?: (item: T) => NamedItem;
  filter?: (item: T) => boolean;
}

export interface DictionaryHandlers<T extends { name: string }> {
  command: string;
  callbackPattern: RegExp;
  sendPage: (
    ctx: MyContext,
    prefix: string,
    page: number,
    edit?: boolean,
  ) => Promise<void>;
  commandHandler: (ctx: MyContext) => Promise<void>;
  register: (bot: Bot<MyContext>, withRegistered: WithRegistered) => void;
}

function defaultMapItem<T extends { name: string }>(item: T): NamedItem {
  return { name: item.name };
}

export function createDictionaryCommand<T extends { name: string }>(
  options: DictionaryFactoryOptions<T>,
): DictionaryHandlers<T> {
  const { command, fetchAll, errorKey, filter, mapItem = defaultMapItem } = options;

  const callbackPattern = new RegExp(`^${command}:(\\d+):(.*)$`);
  let handlers: DictionaryHandlers<T>;

  const sendPage: DictionaryHandlers<T>['sendPage'] = async (
    ctx,
    prefix,
    page,
    edit = false,
  ) => {
    await sendNamedItemsPage({
      ctx,
      command,
      fetchAll: async () => {
        const items = await Promise.resolve(fetchAll());
        const filteredItems = filter ? items.filter(filter) : items;
        return filteredItems.map((item) => mapItem(item));
      },
      prefix,
      page,
      edit,
      errorMsg: ctx.t(errorKey),
    });
  };

  const commandHandler: DictionaryHandlers<T>['commandHandler'] = async (ctx) => {
    const prefix = parsePrefix(ctx.message?.text);
    await handlers.sendPage(ctx, prefix, 1);
  };

  const register: DictionaryHandlers<T>['register'] = (bot, withRegistered) => {
    bot.command(command, withRegistered(commandHandler));
    bot.callbackQuery(
      callbackPattern,
      withRegistered(async (ctx) => {
        if (!ctx.match || typeof ctx.match === 'string') {
          throw new Error('Callback query match is undefined.');
        }
        const page = parseInt(ctx.match[1]!, 10);
        const prefix = decodeURIComponent(ctx.match[2]!);
        await ctx.answerCallbackQuery();
        await handlers.sendPage(ctx, prefix, page, true);
      }),
    );
  };

  handlers = {
    command,
    callbackPattern,
    sendPage,
    commandHandler,
    register,
  };

  return handlers;
}
