import type { Context } from 'grammy';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  runWithRequestContext: vi.fn(),
  handleServiceError: vi.fn(),
}));

vi.mock('../src/api/client', () => ({
  runWithRequestContext: mocks.runWithRequestContext,
}));

vi.mock('../src/errorHandler', () => ({
  handleServiceError: mocks.handleServiceError,
}));

const { runWithRequestContext, handleServiceError } = mocks;

import { callWithContext } from '../src/services/call-with-context';

describe('callWithContext', () => {
  const ctx = { from: { id: 42 } } as unknown as Context;

  beforeEach(() => {
    runWithRequestContext.mockReset();
    handleServiceError.mockReset();
    runWithRequestContext.mockImplementation(async (_ctx, cb) => {
      return await (cb as () => Promise<unknown> | unknown)();
    });
  });

  it('sets the context, forwards arguments, and returns the result', async () => {
    const fn = vi.fn<[string, number], Promise<string>>().mockResolvedValue('ok');

    await expect(callWithContext(ctx, fn, 'value', 123)).resolves.toBe('ok');

    expect(runWithRequestContext).toHaveBeenCalledTimes(1);
    expect(runWithRequestContext).toHaveBeenCalledWith(ctx, expect.any(Function));
    expect(fn).toHaveBeenCalledWith('value', 123);
    expect(handleServiceError).not.toHaveBeenCalled();
  });

  it('funnels errors through handleServiceError and rethrows', async () => {
    const error = new Error('boom');
    const fn = vi.fn<[], Promise<void>>().mockRejectedValue(error);

    await expect(callWithContext(ctx, fn)).rejects.toBe(error);

    expect(runWithRequestContext).toHaveBeenCalledTimes(1);
    expect(runWithRequestContext).toHaveBeenCalledWith(ctx, expect.any(Function));
    expect(handleServiceError).toHaveBeenCalledTimes(1);
    expect(handleServiceError).toHaveBeenCalledWith(error);
  });
});
