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
    const loginMock = vi.fn().mockResolvedValue({ token: 't' });
    const setToken = vi.fn();
    vi.doMock('@photobank/shared/generated', () => ({
      AuthService: { postApiAuthLogin: loginMock },
      OpenAPI: {},
    }));
    vi.doMock('@photobank/shared/auth', () => ({ setAuthToken: setToken }));
    const { loginUser } = await import('../src/features/auth/model/authSlice');
    const dispatch = vi.fn();
    const getState = vi.fn();
    await loginUser({ email: 'a', password: 'b' })(dispatch, getState, undefined);
    expect(loginMock).toHaveBeenCalledWith({ email: 'a', password: 'b' });
    expect(setToken).toHaveBeenCalledWith('t', true);
  });
});
