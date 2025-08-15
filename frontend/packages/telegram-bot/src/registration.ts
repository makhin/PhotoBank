import { Context } from 'grammy';
import { notRegisteredMsg } from '@photobank/shared/constants';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { ensureUserAccessToken } from './auth';

export async function ensureRegistered(ctx: Context): Promise<boolean> {
  try {
    await ensureUserAccessToken(ctx);
    return true;
  } catch (err) {
    if (err instanceof ProblemDetailsError && err.status === 403) {
      await ctx.reply(
        'Ваш Telegram не привязан к аккаунту PhotoBank. Обратитесь к администратору.',
      );
    } else {
      await ctx.reply(notRegisteredMsg);
    }
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
