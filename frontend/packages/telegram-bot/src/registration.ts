import { Context } from 'grammy';
import { getCurrentUser } from '@photobank/shared/api/auth';
import { notRegisteredMsg } from '@photobank/shared/constants';

export async function ensureRegistered(ctx: Context): Promise<boolean> {
  try {
    await getCurrentUser();
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
