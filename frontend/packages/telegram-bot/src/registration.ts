import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { ensureUserAccessToken } from './auth';
import type { MyContext } from './i18n';
import type { MiddlewareFn } from 'grammy';

export async function ensureRegistered(ctx: MyContext): Promise<boolean> {
  try {
    await ensureUserAccessToken(ctx);
    return true;
  } catch (err) {
    if (err instanceof ProblemDetailsError && err.problem.status === 403) {
      await ctx.reply(ctx.t('not-registered'));
    } else {
      await ctx.reply(ctx.t('not-registered'));
    }
    return false;
  }
}

export function withRegistered<T extends MyContext>(handler: (ctx: T) => Promise<void>): MiddlewareFn<T> {
  return async (ctx, next) => {
    if (await ensureRegistered(ctx as MyContext)) {
      await handler(ctx);
    }
    return next();
  };
}
