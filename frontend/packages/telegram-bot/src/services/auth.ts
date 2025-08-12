import {
  authGetUser,
  authLogin,
  authGetUserRoles,
  authGetUserClaims,
} from '@photobank/shared/api/photobank';
import { handleServiceError } from '../errorHandler';

export async function login(email: string, password: string) {
  try {
    return await authLogin({ email, password });
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getUser() {
  try {
    return await authGetUser();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getUserRoles() {
  try {
    return await authGetUserRoles();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}

export async function getUserClaims() {
  try {
    return await authGetUserClaims();
  } catch (err) {
    handleServiceError(err);
    throw err;
  }
}
