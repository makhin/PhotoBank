import { Context } from 'grammy';
import { notRegisteredMsg } from '@photobank/shared/constants';

import { getUser } from './services/auth';

export async function ensureRegistered(ctx: Context): Promise<boolean> {
  try {
    await getUser();
    return true;
  } catch {
    await ctx.reply(notRegisteredMsg);
    return false;
  }
}

export function withRegistered(handler: (ctx: Context) => Promise<void>) {
  return async (ctx: Context) => {
    if (await ensureRegistered(ctx)) {
      await handler(ctx);
    }
  };
}
