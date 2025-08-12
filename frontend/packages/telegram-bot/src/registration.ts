import { Context } from 'grammy';
import { authGetUser } from '@photobank/shared/src/api/photobank';
import { notRegisteredMsg } from '@photobank/shared/constants';

export async function ensureRegistered(ctx: Context): Promise<boolean> {
  try {
    await authGetUser();
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
