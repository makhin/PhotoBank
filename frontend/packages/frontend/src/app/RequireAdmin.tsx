import { Navigate, Outlet, useLocation } from 'react-router-dom';
import * as AuthApi from '@photobank/shared/api/photobank/auth/auth';

export default function RequireAdmin() {
  const location = useLocation();
  const { data: roles, isLoading } = AuthApi.useAuthGetUserRoles();

  if (isLoading) return null;
  const isAdmin = roles?.data.some((r) => r.name === 'Administrator');
  if (!isAdmin) return <Navigate to="/filter" state={{ from: location }} replace />;
  return <Outlet />;
}
