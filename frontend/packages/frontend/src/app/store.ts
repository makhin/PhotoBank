import { configureStore } from '@reduxjs/toolkit';

import metaReducer from '@/features/meta/model/metaSlice';
import photoReducer from '@/features/photo/model/photoSlice';
import botReducer from '@/features/bot/model/botSlice';
import authReducer from '@/features/auth/model/authSlice';

export const store = configureStore({
  reducer: {
    metadata: metaReducer,
    photo: photoReducer,
    bot: botReducer,
    auth: authReducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware(),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
