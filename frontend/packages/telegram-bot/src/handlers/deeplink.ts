import { bot } from '../bot';
import type { MyContext } from '../i18n';

bot.on('message', async (ctx: MyContext, next) => {
  const text = ctx.message?.text ?? '';
  const m = /^\/start(?:\s+(\S+))?/.exec(text);
  if (m) {
    const param = m[1];
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
