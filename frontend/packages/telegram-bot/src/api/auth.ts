import { ProblemDetailsError } from '@photobank/shared/types/problem';

const API_BASE_URL = process.env.API_BASE_URL!;
const BOT_SERVICE_KEY = process.env.BOT_SERVICE_KEY!;

export async function exchangeTelegramUserToken(telegramUserId: number, username?: string) {
  const res = await fetch(`${API_BASE_URL}/api/auth/telegram/exchange`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Service-Key': BOT_SERVICE_KEY,
    },
    body: JSON.stringify({ telegramUserId, username: username ?? null }),
  });
  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    if (problem?.title && problem?.status) throw new ProblemDetailsError(problem);
    throw new Error(`HTTP ${res.status}`);
  }
  return res.json() as Promise<{ accessToken: string; expiresIn: number }>;
}
