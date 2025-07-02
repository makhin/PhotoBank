import axios from 'axios';
import {API_BASE_URL, isBrowser} from "@photobank/shared/config";
import {getAuthToken} from './auth';

let impersonateUser: string | null = null;
export const setImpersonateUser = (username: string | null | undefined) => {
  impersonateUser = username ?? null;
};

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api/`,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const token = getAuthToken();
  if (token) {
    config.headers = config.headers ?? {};
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  if (impersonateUser) {
    config.headers = config.headers ?? {};
    config.headers['X-Impersonate-User'] = impersonateUser;
  }
  return config;
});

apiClient.interceptors.response.use(undefined, (error) => {
  if (
    error.response &&
    (error.response.status === 401 || error.response.status === 403)
  ) {
    if (isBrowser) {
      console.warn('Unauthorized request, redirecting to login');
      window.location.href = '/login';
    }
  }
  return Promise.reject(error);
});
