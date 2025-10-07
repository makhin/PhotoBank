import {
  configureApi as setBaseUrl,
  configureApiAuth,
} from '@photobank/shared/api/photobank';
import { getAuthToken } from '@photobank/shared/auth';

export function configureApi(baseUrl: string) {
  setBaseUrl(baseUrl);
  configureApiAuth({
    getToken: () => getAuthToken() ?? undefined,
  });
}
