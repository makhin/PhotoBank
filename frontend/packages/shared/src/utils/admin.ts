import { getAuthToken } from '../auth';
import { authGetUserRoles } from '../api/photobank';

export const checkIsAdmin = async (): Promise<boolean> => {
  if (!getAuthToken()) return false;
  try {
    const { data: roles } = await authGetUserRoles();
    return roles.some((r) => r.name === 'Administrator');
  } catch {
    return false;
  }
};
