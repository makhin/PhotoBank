import { configureStore, type Middleware } from '@reduxjs/toolkit';
import { namespacedStorage } from '@photobank/shared/safeStorage';
import { PHOTO_FILTER_STORAGE_KEY } from '@photobank/shared/constants';

import metaReducer from '@/features/meta/model/metaSlice';
import photoReducer, {
  setFilter,
  resetFilter,
} from '@/features/photo/model/photoSlice';
import botReducer from '@/features/bot/model/botSlice';
import authReducer from '@/features/auth/model/authSlice';

const filterStore = namespacedStorage('photo');

const filterMiddleware: Middleware = (storeAPI) => (next) => (action) => {
  const result = next(action);
  if (setFilter.match(action) || resetFilter.match(action)) {
    const filter = storeAPI.getState().photo.filter;
    filterStore.set(PHOTO_FILTER_STORAGE_KEY, filter);
  }
  return result;
};

export const store = configureStore({
  reducer: {
    metadata: metaReducer,
    photo: photoReducer,
    bot: botReducer,
    auth: authReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(filterMiddleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
