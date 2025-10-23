import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('authSlice', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
  });

  it('resetError clears error', async () => {
    const { resetError, default: reducer } = await import('../src/features/auth/model/authSlice');
    const state = reducer({ loading: false, error: 'err' } as any, resetError());
    expect(state.error).toBeUndefined();
  });

  it('loginUser calls api (migrated to TanStack Query)', async () => {
    /* TODO */
  });
});
