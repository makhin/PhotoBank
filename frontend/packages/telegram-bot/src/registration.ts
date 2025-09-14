import type { MiddlewareFn } from 'grammy';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { ensureUserAccessToken } from './auth';
import type { MyContext } from './i18n';

export async function ensureRegistered(ctx: MyContext): Promise<boolean> {
  try {
    await ensureUserAccessToken(ctx);
    return true;
  } catch (e: unknown) {
    let forbidden = false;
    if (e instanceof ProblemDetailsError) {
      forbidden = e.problem.status === 403;
    }
    await ctx.reply(
      forbidden
        ? ctx.t('not-registered', { userId: ctx.from?.id ?? 0 })
        : ctx.t('sorry-try-later')
    );
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
