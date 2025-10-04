import type { Context } from 'grammy';

import { exchangeTelegramUserToken } from './api/auth';

type CacheEntry = { token: string; exp: number }; // seconds since epoch
const tokenCache = new Map<string, CacheEntry>();

export async function ensureUserAccessToken(ctx: Context, force = false): Promise<string> {
  const tgIdRaw = ctx.from?.id;
  if (tgIdRaw === undefined || tgIdRaw === null) {
    throw new Error('No Telegram user');
  }
  const tgId = tgIdRaw.toString();
  const now = Math.floor(Date.now() / 1000);
  const cached = force ? undefined : tokenCache.get(tgId);
  if (cached && cached.exp - now > 60) return cached.token;

  const { accessToken, expiresIn } = await exchangeTelegramUserToken(tgId, ctx.from?.username);
  tokenCache.set(tgId, { token: accessToken, exp: now + Math.max(60, Math.min(expiresIn, 3600)) });
  return accessToken;
}

export function invalidateUserToken(ctx: Context | { from?: { id?: number } }) {
  const tgIdRaw = ctx.from?.id;
  if (tgIdRaw === undefined || tgIdRaw === null) {
    return;
  }
  tokenCache.delete(tgIdRaw.toString());
}
