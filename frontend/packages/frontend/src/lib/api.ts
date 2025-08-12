import {
  configureApi as setBaseUrl,
  configureApiAuth,
} from '@photobank/shared/api/photobank/fetcher';
import { getAuthToken } from '@photobank/shared/auth';

export function configureApi(baseUrl: string) {
  setBaseUrl(baseUrl);
  configureApiAuth(() => getAuthToken() ?? undefined);
}
