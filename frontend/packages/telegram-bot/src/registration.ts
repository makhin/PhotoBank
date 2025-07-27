import { Context } from 'grammy';
import { AuthService } from '@photobank/shared/generated';
import { notRegisteredMsg } from '@photobank/shared/constants';

export async function ensureRegistered(ctx: Context): Promise<boolean> {
  try {
    await AuthService.getApiAuthUser();
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
