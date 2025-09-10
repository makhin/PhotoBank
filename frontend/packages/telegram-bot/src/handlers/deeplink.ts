import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { bot } from '../bot.js';
import type { MyContext } from '../i18n.js';
import { ensureUserAccessToken } from '../auth.js';

bot.on('message', async (ctx: MyContext, next) => {
  const text = ctx.message?.text ?? '';
  const m = /^\/start(?:\s+(\S+))?/.exec(text);
  if (m) {
    const param = m[1];
    // Базовый /start — пробуем авторизацию по-новому
    if (!param) {
      try {
        await ensureUserAccessToken(ctx);
        await ctx.reply(ctx.t('start-linked'));
      } catch (e) {
        const forbidden = e instanceof ProblemDetailsError && e.problem.status === 403;
        await ctx.reply(
          forbidden
            ? ctx.t('not-registered', { userId: ctx.from?.id })
            : ctx.t('sorry-try-later')
        );
      }
      return;
    }
    if (param === 'link') {
      await ctx.reply(ctx.t('deeplink-not-linked'));
      return;
    }
    if (param === 'help') {
      await ctx.reply(ctx.t('deeplink-inline-example'));
      return;
    }
  }
  return next();
});
