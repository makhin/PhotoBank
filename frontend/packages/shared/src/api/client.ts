import { OpenAPI } from '../generated';
import { getAuthToken } from './auth';

let impersonateUser: string | null = null;
export const setImpersonateUser = (username: string | null | undefined) => {
  impersonateUser = username ?? null;
};

OpenAPI.WITH_CREDENTIALS = true;
export function setApiBaseUrl(url: string) {
  OpenAPI.BASE = url;
}

OpenAPI.TOKEN = async () => getAuthToken() ?? '';
OpenAPI.HEADERS = async (_options): Promise<Record<string, string>> => {
  return impersonateUser ? { 'X-Impersonate-User': impersonateUser } : {};
};