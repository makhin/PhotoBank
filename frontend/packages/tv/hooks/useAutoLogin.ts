import { useEffect } from 'react';
import { authLogin } from '@photobank/shared/api/photobank';
import { setAuthToken } from '@photobank/shared/auth';
import { logger } from '@photobank/shared/utils/logger';

import {API_EMAIL, API_PASSWORD} from '../config';

export function useAutoLogin() {
  useEffect(() => {
    async function doLogin() {
      try {
        logger.debug('Logging in with default credentials');
        const res = await authLogin({
          email: API_EMAIL,
          password: API_PASSWORD,
        });
        setAuthToken(res.data.token!, true);
        logger.debug('Login success');
      } catch (e: unknown) {
        logger.error('Login failed', e);
      }
    }

    void doLogin();
  }, []);
}
