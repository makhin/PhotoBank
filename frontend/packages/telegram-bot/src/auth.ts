import type { Context } from 'grammy';
import { exchangeTelegramUserToken } from './api/auth';

type CacheEntry = { token: string; exp: number }; // seconds since epoch
const tokenCache = new Map<number, CacheEntry>();

export async function ensureUserAccessToken(ctx: Context): Promise<string> {
  const tgId = ctx.from?.id;
  if (!tgId) throw new Error('No Telegram user');
  const now = Math.floor(Date.now() / 1000);
  const cached = tokenCache.get(tgId);
  if (cached && cached.exp - now > 60) return cached.token;

  const { accessToken, expiresIn } = await exchangeTelegramUserToken(tgId, ctx.from?.username);
  tokenCache.set(tgId, { token: accessToken, exp: now + Math.max(60, Math.min(expiresIn, 3600)) });
  return accessToken;
}
