import type { TelegramSubscriptionDto } from '@photobank/shared/api/photobank';
import { customFetcher } from '@photobank/shared/api/photobank/fetcher';

import { BOT_SERVICE_KEY } from '../config';

import './client';

const serviceKey = BOT_SERVICE_KEY ?? '';

type ExchangeBody = { telegramUserId: string; username: string | null; languageCode: string | null };

export async function exchangeTelegramUserToken(telegramUserId: string, username?: string, languageCode?: string) {
  const body: ExchangeBody = { telegramUserId, username: username ?? null, languageCode: languageCode ?? null };
  const res = await customFetcher<{ data: { accessToken: string; expiresIn: number } }>(
    '/auth/telegram/exchange',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Service-Key': serviceKey,
      },
      body: JSON.stringify(body),
    },
  );
  return res.data;
}

export async function fetchTelegramSubscriptions(): Promise<TelegramSubscriptionDto[]> {
  const res = await customFetcher<{ data: TelegramSubscriptionDto[] }>(
    '/auth/telegram/subscriptions',
    {
      method: 'GET',
      headers: {
        'X-Service-Key': serviceKey,
      },
    },
  );
  return res.data ?? [];
}
