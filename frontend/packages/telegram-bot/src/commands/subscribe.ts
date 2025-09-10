import { Bot } from 'grammy';
import type { UpdateUserDto } from '@photobank/shared/api/photobank';

import { updateUser } from '../services/auth.js';
import type { MyContext } from '../i18n.js';
import { i18n } from '../i18n.js';
import { sendThisDayPage } from './thisday.js';

export const subscriptions = new Map<number, { time: string; locale: string }>();

export function parseSubscribeTime(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/\/subscribe\s+(\d{1,2}:\d{2})/);
  if (!match) return null;
  const [hours, minutes] = match[1].split(":").map(Number);
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
  const dto: UpdateUserDto & { telegramSendTimeUtc: string } = {
    telegramSendTimeUtc: `${time}:00`,
  };
  await updateUser(ctx, dto);
  const locale = await ctx.i18n.getLocale();
  subscriptions.set(ctx.chat.id, { time, locale });
  await ctx.reply(ctx.t('subscription-confirmed', { time }));
}

export function initSubscriptionScheduler(bot: Bot<MyContext>) {
  setInterval(() => {
    (async () => {
      const now = new Date();
      const current = `${String(now.getUTCHours()).padStart(2, "0")}:${String(now.getUTCMinutes()).padStart(2, "0")}`;
      for (const [chatId, info] of subscriptions.entries()) {
        if (info.time === current) {
          const ctxLike = {
            message: { text: '/thisday' },
            reply: (text: string, opts?: Record<string, unknown>) =>
              bot.api.sendMessage(chatId, text, opts),
            t: (key: string) => i18n.t(info.locale, key),
            i18n: { getLocale: () => info.locale } as unknown as MyContext['i18n'],
          } as unknown as MyContext;
          await sendThisDayPage(ctxLike, 1);
        }
      }
    })();
  }, 60 * 1000);
}