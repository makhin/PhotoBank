import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getAuthToken } from '@photobank/shared/api';

export default function RequireAuth() {
  const location = useLocation();
  const token = getAuthToken();

  if (!token) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
