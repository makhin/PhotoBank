export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
}

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

import { apiClient } from './client';

export const login = async (data: LoginRequest): Promise<LoginResponse> => {
  const response = await apiClient.post<LoginResponse>('/auth/login', data);
  setAuthToken(response.data.token);
  return response.data;
};

export const logout = () => {
  clearAuthToken();
};
