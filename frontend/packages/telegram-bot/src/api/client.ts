import type { Context } from 'grammy';

import { configureApi, getRequestContext, runWithRequestContext } from '@photobank/shared/api/photobank';
import { applyHttpContext } from '@photobank/shared/api/photobank/httpContext';

import { ensureUserAccessToken, invalidateUserToken } from '@/auth';

const API_BASE_URL = process.env.API_BASE_URL ?? '';

configureApi(API_BASE_URL);

applyHttpContext({
  auth: {
    getToken: async (ctx, options) => {
      const context = (ctx ?? getRequestContext<Context>()) as Context | undefined;
      if (!context) return undefined;
      return ensureUserAccessToken(context, options?.forceRefresh ?? false);
    },
    onAuthError: async (ctx) => {
      const context = (ctx ?? getRequestContext<Context>()) as Context | undefined;
      if (context) invalidateUserToken(context);
    },
  },
});

export { runWithRequestContext };
