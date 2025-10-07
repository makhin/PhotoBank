import { configureApi as setBaseUrl } from '@photobank/shared/api/photobank';
import { applyHttpContext } from '@photobank/shared/api/photobank/httpContext';
import { getAuthToken } from '@photobank/shared/auth';

export function configureApi(baseUrl: string) {
  setBaseUrl(baseUrl);
  applyHttpContext({
    auth: {
      getToken: () => getAuthToken() ?? undefined,
    },
  });
}
