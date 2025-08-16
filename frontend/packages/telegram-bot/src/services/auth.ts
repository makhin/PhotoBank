import type { Context } from 'grammy';
import {
  authGetUser,
  authGetUserRoles,
  authGetUserClaims,
  type authGetUserResponse,
  type authGetUserRolesResponse,
  type authGetUserClaimsResponse,
} from '@photobank/shared/api/photobank';

import { ensureUserAccessToken } from '../auth';

async function authorized<T>(ctx: Context, fn: (options?: RequestInit) => Promise<T>): Promise<T> {
  const token = await ensureUserAccessToken(ctx);
  return fn({ headers: { Authorization: `Bearer ${token}` } });
}

export function getUser(ctx: Context): Promise<authGetUserResponse> {
  return authorized(ctx, authGetUser);
}

export function getUserRoles(ctx: Context): Promise<authGetUserRolesResponse> {
  return authorized(ctx, authGetUserRoles);
}

export function getUserClaims(ctx: Context): Promise<authGetUserClaimsResponse> {
  return authorized(ctx, authGetUserClaims);
}
