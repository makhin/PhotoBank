import type { Context } from 'grammy';

import { setRequestContext } from '../api/axios-instance';
import { handleServiceError } from '../errorHandler';

export async function callWithContext<Fn extends (...args: any[]) => unknown>(
  ctx: Context,
  fn: Fn,
  ...args: Parameters<Fn>
): Promise<Awaited<ReturnType<Fn>>> {
  try {
    setRequestContext(ctx);
    return await fn(...args);
  } catch (error: unknown) {
    handleServiceError(error);
    throw error;
  }
}
