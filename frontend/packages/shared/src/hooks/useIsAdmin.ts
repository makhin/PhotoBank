import { useEffect, useState } from 'react';
import { checkIsAdmin } from '../utils/admin';

export const useIsAdmin = (): boolean | null => {
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);
  useEffect(() => {
    checkIsAdmin().then(setIsAdmin).catch(() => setIsAdmin(false));
  }, []);
  return isAdmin;
};
