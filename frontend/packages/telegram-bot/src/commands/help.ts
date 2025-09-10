import type { MyContext } from '../i18n.js';

export async function helpCommand(ctx: MyContext) {
  await ctx.reply(ctx.t('help'));
}

