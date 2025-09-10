import { configureApi, customFetcher } from '@photobank/shared/api/photobank';

import { BOT_SERVICE_KEY } from '../config';

configureApi(process.env.API_BASE_URL ?? '');

const serviceKey = BOT_SERVICE_KEY ?? '';

type ExchangeBody = { telegramUserId: number; username: string | null };

export async function exchangeTelegramUserToken(telegramUserId: number, username?: string) {
  const body: ExchangeBody = { telegramUserId, username: username ?? null };
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
