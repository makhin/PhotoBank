import { Context } from 'grammy';
import { helpBotMsg } from '@photobank/shared/constants';

export async function helpCommand(ctx: Context) {
  await ctx.reply(helpBotMsg);
}

