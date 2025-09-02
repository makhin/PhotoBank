import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useIsAdmin } from '@photobank/shared';

export default function RequireAdmin() {
  const location = useLocation();
  const isAdmin = useIsAdmin();

  if (isAdmin === null) return null;
  if (!isAdmin) return <Navigate to="/filter" state={{ from: location }} replace />;
  return <Outlet />;
}
