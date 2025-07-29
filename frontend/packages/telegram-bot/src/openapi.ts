import { OpenAPI } from '@photobank/shared/generated';
import { getAuthToken } from '@photobank/shared/api/auth';

let impersonateUser: string | null = null;
export function setImpersonateUser(username: string | null | undefined) {
  impersonateUser = username ?? null;
}

export function configureApi(baseUrl: string) {
  OpenAPI.BASE = baseUrl;
  OpenAPI.WITH_CREDENTIALS = true;
  OpenAPI.TOKEN = async () => getAuthToken() ?? '';
  OpenAPI.HEADERS = async () =>
    impersonateUser ? { 'X-Impersonate-User': impersonateUser } : {};
}
