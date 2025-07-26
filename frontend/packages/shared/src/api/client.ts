import axios from 'axios';
import { isBrowser } from '@photobank/shared/config';
import { OpenAPI } from '../generated';
import { getAuthToken } from './auth';

let impersonateUser: string | null = null;
export const setImpersonateUser = (username: string | null | undefined) => {
  impersonateUser = username ?? null;
};

OpenAPI.WITH_CREDENTIALS = true;
axios.defaults.timeout = 10000;

export function setApiBaseUrl(url: string) {
  OpenAPI.BASE = url;
  axios.defaults.baseURL = url;
}

OpenAPI.TOKEN = async () => getAuthToken() ?? '';
OpenAPI.HEADERS = async () =>
  impersonateUser ? { 'X-Impersonate-User': impersonateUser } : {};

axios.interceptors.response.use(undefined, (error) => {
  if (
    error.response &&
    (error.response.status === 401 || error.response.status === 403)
  ) {
    if (isBrowser()) {
      console.warn('Unauthorized request, redirecting to login');
      window.location.href = '/login';
    }
  }
  return Promise.reject(error);
});
