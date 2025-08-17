import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { ensureUserAccessToken } from './auth';
import type { MyContext } from './i18n';

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

export function withRegistered(handler: (ctx: MyContext) => Promise<void>) {
  return async (ctx: MyContext) => {
    if (await ensureRegistered(ctx)) {
      await handler(ctx);
    }
  };
}
