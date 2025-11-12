import { authService } from '@/services/authService';
import { useAppStore } from '@/store/useAppStore';

jest.mock('@/api/generated/auth/auth', () => ({
  authLogin: jest.fn(),
}));

jest.mock('@/api/client', () => ({
  authHelpers: {
    getToken: jest.fn(),
    saveToken: jest.fn(),
    removeToken: jest.fn(),
  },
}));

jest.mock('@/config/authConfig', () => ({
  authConfig: {
    loadEnvironmentCredentials: jest.fn(),
  },
}));

jest.mock('@react-native-async-storage/async-storage', () => ({
  __esModule: true,
  default: {
    setItem: jest.fn(),
    getItem: jest.fn(),
    removeItem: jest.fn(),
    clear: jest.fn(),
    getAllKeys: jest.fn(),
    multiGet: jest.fn(),
    multiSet: jest.fn(),
    multiRemove: jest.fn(),
  },
}));

const { authLogin } = require('@/api/generated/auth/auth');
const { authHelpers } = require('@/api/client');
const { authConfig } = require('@/config/authConfig');

describe('authService.autoLogin', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    useAppStore.setState({
      credentials: {
        username: '',
        password: '',
      },
    });
  });

  it('returns missing-credentials when no credentials are stored', async () => {
    (authHelpers.getToken as jest.Mock).mockResolvedValue(null);
    (authConfig.loadEnvironmentCredentials as jest.Mock).mockReturnValue(null);

    const result = await authService.autoLogin();

    expect(result).toEqual({ status: 'missing-credentials' });
    expect(authLogin).not.toHaveBeenCalled();
  });

  it('returns failure when login throws an error', async () => {
    useAppStore.setState({
      credentials: {
        username: 'user@example.com',
        password: 'secret',
      },
    });

    (authHelpers.getToken as jest.Mock).mockResolvedValue(null);
    (authConfig.loadEnvironmentCredentials as jest.Mock).mockReturnValue(null);
    (authLogin as jest.Mock).mockRejectedValue(new Error('Invalid credentials'));

    const result = await authService.autoLogin();

    expect(result.status).toBe('failure');
    expect(result.message).toBe('Invalid credentials');
  });
});
