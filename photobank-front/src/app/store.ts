import {configureStore} from "@reduxjs/toolkit";
import metaReducer from '@/features/meta/metaSlice';
import {photobankApi} from "@/features/api/photobankApi.ts";

export const store = configureStore({
    reducer: {
        metadata: metaReducer,
        [photobankApi.reducerPath]: photobankApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(photobankApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
