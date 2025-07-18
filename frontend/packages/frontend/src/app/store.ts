import { configureStore } from '@reduxjs/toolkit';

import metaReducer from '@/features/meta/model/metaSlice.ts';
import photoReducer from '@/features/photo/model/photoSlice.ts';
import botReducer from '@/features/bot/model/botSlice.ts';
import { api } from '@/entities/photo/api.ts';

export const store = configureStore({
  reducer: {
    metadata: metaReducer,
    photo: photoReducer,
    bot: botReducer,
    [api.reducerPath]: api.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
