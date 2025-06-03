// src/app/store.ts
import { configureStore } from '@reduxjs/toolkit';
import photoReducer from '../features/photos/photoSlice';
import metaReducer from '../features/meta/metaSlice';

export const store = configureStore({
  reducer: {
    photos: photoReducer,
    meta: metaReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;