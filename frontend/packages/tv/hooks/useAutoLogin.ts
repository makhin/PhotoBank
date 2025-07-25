import { useEffect } from 'react';
import { login } from '@photobank/shared/api';

import {API_EMAIL, API_PASSWORD} from '../config';

export function useAutoLogin() {
  useEffect(() => {
    async function doLogin() {
      try {
        console.log('Logging in with default credentials');
        await login({ email: API_EMAIL, password: API_PASSWORD });
        console.log('Login success');
      } catch (e) {
        console.error('Login failed', e);
      }
    }

    void doLogin();
  }, []);
}
