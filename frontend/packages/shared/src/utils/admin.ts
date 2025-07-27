import { getAuthToken } from '../api';
import { AuthService } from '../generated';

export const checkIsAdmin = async (): Promise<boolean> => {
  if (!getAuthToken()) return false;
  try {
    const roles = await AuthService.getApiAuthRoles();
    return roles.some((r) => r.name === 'Administrator');
  } catch {
    return false;
  }
};
