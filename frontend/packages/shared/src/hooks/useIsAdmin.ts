import { useEffect, useState } from 'react';

import { checkIsAdmin } from '../utils/admin';

export const useIsAdmin = (): boolean | null => {
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const v = await checkIsAdmin();
        if (!cancelled) setIsAdmin(v);
      } catch {
        if (!cancelled) setIsAdmin(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);
  return isAdmin;
};
