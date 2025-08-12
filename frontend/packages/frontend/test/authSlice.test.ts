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

  it('loginUser calls api', async () => {
    const setToken = vi.fn();
    vi.doMock('@photobank/shared/auth', () => ({ setAuthToken: setToken }));
    const { loginUser } = await import('../src/features/auth/model/authSlice');
    const dispatch = vi
      .fn()
      .mockReturnValue({ unwrap: () => Promise.resolve({ token: 't' }) });
    const getState = vi.fn();
    await loginUser({ email: 'a', password: 'b' })(dispatch, getState, undefined);
    expect(dispatch).toHaveBeenCalled();
    expect(setToken).toHaveBeenCalledWith('t', true);
  });
});
