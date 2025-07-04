import type {
  LoginRequestDto,
  LoginResponseDto,
  RegisterRequestDto,
  UserDto,
  UpdateUserDto,
  ClaimDto,
} from '../types';
import { apiClient } from './client';

const AUTH_TOKEN_KEY = 'photobank_token';
let authToken: string | null = null;

export const getAuthToken = () => authToken;

export const setAuthToken = (token: string) => {
  authToken = token;
  if (typeof window !== 'undefined') {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
  }
};

export const clearAuthToken = () => {
  authToken = null;
  if (typeof window !== 'undefined') {
    localStorage.removeItem(AUTH_TOKEN_KEY);
  }
};

export const loadAuthToken = () => {
  if (typeof window !== 'undefined') {
    const saved = localStorage.getItem(AUTH_TOKEN_KEY);
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
  const response = await apiClient.post<LoginResponseDto>(
    '/auth/login',
    data,
  );
  setAuthToken(response.data.token);
  return response.data;
};

export const register = async (
  data: RegisterRequestDto,
): Promise<void> => {
  await apiClient.post('/auth/register', data);
};

export const getCurrentUser = async (): Promise<UserDto> => {
  const response = await apiClient.get<UserDto>('/auth/user');
  return response.data;
};

export const updateUser = async (
  data: UpdateUserDto,
): Promise<void> => {
  await apiClient.put('/auth/user', data);
};

export const getUserClaims = async (): Promise<ClaimDto[]> => {
  const response = await apiClient.get<ClaimDto[]>('/auth/claims');
  return response.data;
};

export const logout = () => {
  clearAuthToken();
};
