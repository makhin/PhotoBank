import type { Context } from 'grammy';

import { setRequestContext } from '../api/axios-instance';
import { handleServiceError } from '../errorHandler';

type Awaitable<T> = T | PromiseLike<T>;

export function callWithContext<Fn extends (...args: any[]) => Awaitable<unknown>>(
  ctx: Context,
  fn: Fn,
  ...args: Parameters<Fn>
): Promise<Awaited<ReturnType<Fn>>>;

export async function callWithContext(
  ctx: Context,
  fn: (...args: any[]) => Awaitable<unknown>,
  ...args: any[]
): Promise<unknown> {
  try {
    setRequestContext(ctx);
    return await fn(...args);
  } catch (error: unknown) {
    handleServiceError(error);
    throw error;
  }
}
