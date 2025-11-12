// src/config/authConfig.ts
import type { LoginRequestDto } from '@/api/generated/photoBankApi.schemas';

export type AuthCredentials = Pick<LoginRequestDto, 'email' | 'password'> & {
  rememberMe?: boolean;
};

type Env = {
  AUTH_EMAIL?: string;
  AUTH_PASSWORD?: string;
  AUTH_REMEMBER_ME?: string;
};

const getProcessEnv = (): Env => {
  const globalEnv =
    (typeof globalThis !== 'undefined' &&
      (globalThis as { process?: { env?: Env } }).process?.env) ||
    {};

  return globalEnv;
};

const parseRememberMe = (value?: string): boolean | undefined => {
  if (value === undefined) {
    return undefined;
  }

  if (value === '1' || value?.toLowerCase() === 'true') {
    return true;
  }

  if (value === '0' || value?.toLowerCase() === 'false') {
    return false;
  }

  return undefined;
};

export const authConfig = {
  loadEnvironmentCredentials(): AuthCredentials | null {
    const env = getProcessEnv();
    const email = env.AUTH_EMAIL?.trim();
    const password = env.AUTH_PASSWORD;

    if (!email || !password) {
      return null;
    }

    const rememberMe = parseRememberMe(env.AUTH_REMEMBER_ME);

    return {
      email,
      password,
      rememberMe,
    };
  },
};
