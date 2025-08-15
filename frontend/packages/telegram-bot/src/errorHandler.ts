import { BotError, Context } from 'grammy';
import { apiErrorMsg, sorryTryToRequestLaterMsg } from '@photobank/shared/constants';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { logger } from './logger';

export function handleBotError(err: BotError<Context>) {
  const ctx = err.ctx;
  const username = ctx.from?.username ?? String(ctx.from?.id ?? '');
  logger.error(`error handling update from ${username}`, err.error);
}

export async function handleCommandError(ctx: Context, error: unknown) {
  if (error instanceof ProblemDetailsError && error.status === 403) {
    await ctx.reply(
      'Ваш Telegram не привязан к аккаунту PhotoBank. Обратитесь к администратору.',
    );
    return;
  }
  logger.error(apiErrorMsg, error);
  await ctx.reply(sorryTryToRequestLaterMsg);
}

export function handleServiceError(error: unknown) {
  logger.error(apiErrorMsg, error);
}

