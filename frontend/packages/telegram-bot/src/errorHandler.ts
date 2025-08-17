import { BotError, Context } from 'grammy';
import { apiErrorMsg } from '@photobank/shared/constants';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import type { MyContext } from './i18n';
import { logger } from './logger';

export function handleBotError(err: BotError<Context>) {
  const ctx = err.ctx;
  const username = ctx.from?.username ?? String(ctx.from?.id ?? '');
  logger.error(`error handling update from ${username}`, err.error);
}

export async function handleCommandError(ctx: MyContext, error: unknown) {
  if (error instanceof ProblemDetailsError && error.problem.status === 403) {
    await ctx.reply(ctx.t('not-registered'));
    return;
  }
  logger.error(apiErrorMsg, error);
  await ctx.reply(ctx.t('sorry-try-later'));
}

export function handleServiceError(error: unknown) {
  logger.error(apiErrorMsg, error);
}

