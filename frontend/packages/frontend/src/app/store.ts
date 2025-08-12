import { configureStore } from '@reduxjs/toolkit';

import metaReducer from '@/features/meta/model/metaSlice.ts';
import photoReducer from '@/features/photo/model/photoSlice.ts';
import botReducer from '@/features/bot/model/botSlice.ts';
import authReducer from '@/features/auth/model/authSlice.ts';
import { photobankApi } from '@/shared/api.ts';

export const store = configureStore({
  reducer: {
    metadata: metaReducer,
    photo: photoReducer,
    bot: botReducer,
    auth: authReducer,
    [photobankApi.reducerPath]: photobankApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(photobankApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
