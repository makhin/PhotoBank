import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { getAuthToken, getUserRoles } from '@photobank/shared/api';

export default function RequireAdmin() {
  const location = useLocation();
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);
  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      setIsAdmin(false);
      return;
    }
    getUserRoles()
      .then((roles) => {
        setIsAdmin(roles.some((r) => r.name === 'Administrator'));
      })
      .catch(() => setIsAdmin(false));
  }, []);

  if (isAdmin === null) return null;
  if (!isAdmin) return <Navigate to="/filter" state={{ from: location }} replace />;
  return <Outlet />;
}
