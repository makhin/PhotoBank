import { OpenAPI } from '@photobank/shared/generated';
import { getAuthToken } from '@photobank/shared/api/auth';

export function configureApi(baseUrl: string) {
  OpenAPI.BASE = baseUrl;
  OpenAPI.WITH_CREDENTIALS = true;
  OpenAPI.TOKEN = async () => getAuthToken() ?? '';
}
