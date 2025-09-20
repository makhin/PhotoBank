import type { Context } from 'grammy';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  setRequestContext: vi.fn(),
  handleServiceError: vi.fn(),
}));

vi.mock('../src/api/axios-instance', () => ({
  setRequestContext: mocks.setRequestContext,
}));

vi.mock('../src/errorHandler', () => ({
  handleServiceError: mocks.handleServiceError,
}));

const { setRequestContext, handleServiceError } = mocks;

import { callWithContext } from '../src/services/call-with-context';

describe('callWithContext', () => {
  const ctx = { from: { id: 42 } } as unknown as Context;

  beforeEach(() => {
    setRequestContext.mockReset();
    handleServiceError.mockReset();
  });

  it('sets the context, forwards arguments, and returns the result', async () => {
    const fn = vi.fn<[string, number], Promise<string>>().mockResolvedValue('ok');

    await expect(callWithContext(ctx, fn, 'value', 123)).resolves.toBe('ok');

    expect(setRequestContext).toHaveBeenCalledTimes(1);
    expect(setRequestContext).toHaveBeenCalledWith(ctx);
    expect(fn).toHaveBeenCalledWith('value', 123);
    expect(handleServiceError).not.toHaveBeenCalled();
  });

  it('funnels errors through handleServiceError and rethrows', async () => {
    const error = new Error('boom');
    const fn = vi.fn<[], Promise<void>>().mockRejectedValue(error);

    await expect(callWithContext(ctx, fn)).rejects.toBe(error);

    expect(setRequestContext).toHaveBeenCalledTimes(1);
    expect(setRequestContext).toHaveBeenCalledWith(ctx);
    expect(handleServiceError).toHaveBeenCalledTimes(1);
    expect(handleServiceError).toHaveBeenCalledWith(error);
  });
});
