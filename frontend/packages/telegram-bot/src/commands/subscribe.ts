import { Bot } from 'grammy';
import type { UpdateUserDto } from '@photobank/shared/api/photobank';

import { updateUser } from '../services/auth';
import type { MyContext } from '../i18n';
import { i18n } from '../i18n';
import { sendThisDayPage } from './thisday';

type SubscriptionInfo = {
  time: string;
  locale: string;
  from: NonNullable<MyContext['from']>;
};

export const subscriptions = new Map<number, SubscriptionInfo>();

export function parseSubscribeTime(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/\/subscribe\s+(\d{1,2}:\d{2})/);
  if (!match?.[1]) return null;
  const [hStr, mStr] = match[1]!.split(":");
  const hours = Number(hStr);
  const minutes = Number(mStr);
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;
  return `${hours.toString().padStart(2, "0")}:${minutes.toString().padStart(2, "0")}`;
}

export async function subscribeCommand(ctx: MyContext) {
  const time = parseSubscribeTime(ctx.message?.text);
  if (!time) {
    await ctx.reply(ctx.t('subscribe-usage'));
    return;
  }
  if (!ctx.chat) {
    await ctx.reply(ctx.t('chat-undetermined'));
    return;
  }
  if (!ctx.from) {
    await ctx.reply(ctx.t('chat-undetermined'));
    return;
  }
  const dto: UpdateUserDto & { telegramSendTimeUtc: string } = {
    telegramSendTimeUtc: `${time}:00`,
  };
  await updateUser(ctx, dto);
  const locale = await ctx.i18n.getLocale();
  const fromSnapshot: NonNullable<MyContext['from']> = { ...ctx.from };
  subscriptions.set(ctx.chat.id, { time, locale, from: fromSnapshot });
  await ctx.reply(ctx.t('subscription-confirmed', { time }));
}

export function initSubscriptionScheduler(bot: Bot<MyContext>) {
  setInterval(() => {
    (async () => {
      const now = new Date();
      const current = `${String(now.getUTCHours()).padStart(2, "0")}:${String(now.getUTCMinutes()).padStart(2, "0")}`;
      for (const [chatId, info] of subscriptions.entries()) {
        if (info.time === current) {
          const from = { ...info.from } as NonNullable<MyContext['from']>;
          const translate = ((key: Parameters<MyContext['t']>[0], params?: Parameters<MyContext['t']>[1]) =>
            i18n.t(info.locale, key, params)) as MyContext['t'];
          const ctxLike = {
            message: { text: '/thisday' },
            chat: { id: chatId } as MyContext['chat'],
            from,
            reply: (text: string, opts?: Record<string, unknown>) =>
              bot.api.sendMessage(chatId, text, opts),
            t: translate,
            i18n: { getLocale: async () => info.locale } as unknown as MyContext['i18n'],
            api: bot.api,
          } as unknown as MyContext;
          await sendThisDayPage(ctxLike, 1);
        }
      }
    })();
  }, 60 * 1000);
}