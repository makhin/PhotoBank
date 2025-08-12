import { useEffect } from 'react';
import { authLogin } from '@photobank/shared/api/photobank';
import { setAuthToken } from '@photobank/shared/auth';

import {API_EMAIL, API_PASSWORD} from '../config';

export function useAutoLogin() {
  useEffect(() => {
    async function doLogin() {
      try {
        console.log('Logging in with default credentials');
        const res = await authLogin({
          email: API_EMAIL,
          password: API_PASSWORD,
        });
        setAuthToken(res.data.token!, true);
        console.log('Login success');
      } catch (e) {
        console.error('Login failed', e);
      }
    }

    void doLogin();
  }, []);
}
