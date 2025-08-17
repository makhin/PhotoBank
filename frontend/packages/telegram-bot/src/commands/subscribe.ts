import { Bot } from "grammy";
import type { MyContext } from "../i18n";
import { i18n } from "../i18n";

import { sendThisDayPage } from "./thisday";
import { updateUser } from "../services/auth";

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
  await updateUser(ctx, { telegramSendTimeUtc: `${time}:00` });
  subscriptions.set(ctx.chat.id, { time, locale: ctx.i18n.locale() });
  await ctx.reply(ctx.t('subscription-confirmed', { time }));
}

export function initSubscriptionScheduler(bot: Bot) {
  setInterval(() => {
    (async () => {
      const now = new Date();
      const current = `${String(now.getUTCHours()).padStart(2, "0")}:${String(now.getUTCMinutes()).padStart(2, "0")}`;
      for (const [chatId, info] of subscriptions.entries()) {
        if (info.time === current) {
          const ctxLike = {
            message: { text: "/thisday" },
            reply: (text: string, opts?: Record<string, unknown>) => bot.api.sendMessage(chatId, text, opts),
            t: (key: string, params?: Record<string, unknown>) => i18n.t(info.locale, key, params),
            i18n: { locale: () => info.locale } as any,
          } as unknown as MyContext;
          await sendThisDayPage(ctxLike, 1);
        }
      }
    })();
  }, 60 * 1000);
}