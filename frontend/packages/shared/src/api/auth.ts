import type {
  LoginRequestDto,
  LoginResponseDto,
  RegisterRequestDto,
  UserDto,
  UpdateUserDto,
  ClaimDto,
  RoleDto,
} from '../generated';
import { AuthService } from '../generated';

const AUTH_TOKEN_KEY = 'photobank_token';
let authToken: string | null = null;

export const getAuthToken = () => authToken;

export const setAuthToken = (token: string, remember = true) => {
  authToken = token;
  if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
    const hasSession = typeof window.sessionStorage !== 'undefined';
    const storage = remember || !hasSession ? window.localStorage : window.sessionStorage;
    storage.setItem(AUTH_TOKEN_KEY, token);
    if (hasSession) {
      const other = storage === window.localStorage ? window.sessionStorage : window.localStorage;
      other.removeItem(AUTH_TOKEN_KEY);
    }
  }
};

export const clearAuthToken = () => {
  authToken = null;
  if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
    window.localStorage.removeItem(AUTH_TOKEN_KEY);
  }
};

export const loadAuthToken = () => {
  if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
    const saved =
      window.localStorage.getItem(AUTH_TOKEN_KEY) ??
      (typeof window.sessionStorage !== 'undefined'
        ? window.sessionStorage.getItem(AUTH_TOKEN_KEY)
        : null);
    if (saved) {
      authToken = saved;
    }
  }
};

// Immediately load token when running in browser environment
loadAuthToken();

export const login = async (
  data: LoginRequestDto,
): Promise<LoginResponseDto> => {
  const response = await AuthService.postApiAuthLogin(data);
  setAuthToken(response.token!, data.rememberMe ?? true);
  return response;
};

export const register = async (
  data: RegisterRequestDto,
): Promise<void> => {
  await AuthService.postApiAuthRegister(data);
};

export const getCurrentUser = async (): Promise<UserDto> => {
  return AuthService.getApiAuthUser();
};

export const updateUser = async (
  data: UpdateUserDto,
): Promise<void> => {
  await AuthService.putApiAuthUser(data);
};

export const getUserClaims = async (): Promise<ClaimDto[]> => {
  return AuthService.getApiAuthClaims();
};

export const getUserRoles = async (): Promise<RoleDto[]> => {
  return AuthService.getApiAuthRoles();
};

export const logout = () => {
  clearAuthToken();
};
