import { getAuthToken, getUserRoles } from '../api';

export const checkIsAdmin = async (): Promise<boolean> => {
  if (!getAuthToken()) return false;
  try {
    const roles = await getUserRoles();
    return roles.some((r) => r.name === 'Administrator');
  } catch {
    return false;
  }
};
