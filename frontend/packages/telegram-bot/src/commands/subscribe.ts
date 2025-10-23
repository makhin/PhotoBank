import { Bot } from 'grammy';
import type { TelegramSubscriptionDto, UpdateUserDto } from '@photobank/shared/api/photobank';

import { updateUser } from '@/services/auth';
import type { MyContext } from '@/i18n';
import { i18n } from '@/i18n';
import { logger } from '@/logger';
import { fetchTelegramSubscriptions } from '@/api/auth';

import { sendThisDayPage } from './thisday';

type SubscriptionInfo = {
  time: string;
  locale: string;
  from: NonNullable<MyContext['from']>;
  chat: NonNullable<MyContext['chat']>;
};

export const subscriptions = new Map<string, SubscriptionInfo>();

const DEFAULT_LOCALE = 'en';

function normalizeSubscriptionTime(value: string | null | undefined): string | null {
  if (!value) return null;
  const trimmed = value.trim();
  const match = trimmed.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
  if (!match) return null;
  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  if (!Number.isInteger(hours) || !Number.isInteger(minutes)) return null;
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
}

async function buildFromSnapshot(
  bot: Bot<MyContext>,
  chatId: string,
): Promise<{ from: NonNullable<MyContext['from']>; chat: NonNullable<MyContext['chat']> }> {
  const baseFrom: NonNullable<MyContext['from']> = {
    id: Number(chatId),
    is_bot: false,
    first_name: 'Telegram user',
  } as NonNullable<MyContext['from']>;

  let chatSnapshot: NonNullable<MyContext['chat']> = {
    id: Number(chatId),
    type: 'private',
  } as NonNullable<MyContext['chat']>;

  const getChat = bot.api?.getChat?.bind(bot.api);
  if (typeof getChat !== 'function') return { from: baseFrom, chat: chatSnapshot };

  try {
    const chat = await getChat(chatId);
    if (chat && typeof chat === 'object') {
      chatSnapshot = chat as NonNullable<MyContext['chat']>;
      if ('first_name' in chat && chat.first_name) {
        baseFrom.first_name = chat.first_name;
      }
      if ('last_name' in chat && chat.last_name) {
        baseFrom.last_name = chat.last_name;
      }
      if ('username' in chat && chat.username) {
        baseFrom.username = chat.username;
      }
    }
  } catch (error) {
    logger.warn('Failed to enrich restored subscription with chat info', chatId, error);
  }

  return { from: baseFrom, chat: chatSnapshot };
}

function isValidSubscription(entry: TelegramSubscriptionDto | undefined): entry is TelegramSubscriptionDto {
  return !!entry && /^\d+$/.test(entry.telegramUserId);
}

export function parseSubscribeTime(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/\/subscribe\s+(\d{1,2}:\d{2})/);
  if (!match?.[1]) return null;
  const [hStr, mStr] = match[1].split(":");
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
  const chatSnapshot: NonNullable<MyContext['chat']> = { ...ctx.chat };
  subscriptions.set(ctx.chat.id.toString(), { time, locale, from: fromSnapshot, chat: chatSnapshot });
  await ctx.reply(ctx.t('subscription-confirmed', { time }));
}

export async function restoreSubscriptions(bot: Bot<MyContext>): Promise<void> {
  let stored: TelegramSubscriptionDto[];
  try {
    stored = await fetchTelegramSubscriptions();
  } catch (error) {
    logger.error('Failed to fetch saved Telegram subscriptions', error);
    return;
  }

  if (!Array.isArray(stored) || stored.length === 0) return;

  for (const entry of stored) {
    if (!isValidSubscription(entry)) {
      logger.warn('Skipping restored subscription due to invalid payload', entry);
      continue;
    }

    const normalizedTime = normalizeSubscriptionTime(entry.telegramSendTimeUtc);
    if (!normalizedTime) {
      logger.warn('Skipping restored subscription due to invalid time', entry);
      continue;
    }

    const { from, chat } = await buildFromSnapshot(bot, entry.telegramUserId);
    const locale = DEFAULT_LOCALE;

    subscriptions.set(entry.telegramUserId, {
      time: normalizedTime,
      locale,
      from,
      chat,
    });
  }
}

export function initSubscriptionScheduler(bot: Bot<MyContext>) {
  setInterval(() => {
    (async () => {
      const now = new Date();
      const current = `${String(now.getUTCHours()).padStart(2, "0")}:${String(now.getUTCMinutes()).padStart(2, "0")}`;
      for (const [chatId, info] of subscriptions.entries()) {
        if (info.time === current) {
          const from = { ...info.from } as NonNullable<MyContext['from']>;
          const chat = { ...info.chat } as NonNullable<MyContext['chat']>;
          const translate = ((key: Parameters<MyContext['t']>[0], params?: Parameters<MyContext['t']>[1]) =>
            i18n.t(info.locale, key, params)) as MyContext['t'];
          const ctxLike = {
            message: { text: '/thisday' },
            chat,
            from,
            reply: (text: string, opts?: Record<string, unknown>) =>
              bot.api.sendMessage(chatId, text, opts),
            t: translate,
            i18n: { getLocale: () => info.locale } as unknown as MyContext['i18n'],
            api: bot.api,
          } as unknown as MyContext;
          await sendThisDayPage(ctxLike, 1);
        }
      }
    })();
  }, 60 * 1000);
}