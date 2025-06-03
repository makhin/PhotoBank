import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { fetchPhotos } from '@/services/photoApi.ts';
import type {FilterDto, QueryResult} from '@/types/api';

interface PhotoState {
    items: QueryResult["photos"] | [];
    count: number;
    loading: boolean;
}

const initialState: PhotoState = {
    items: [],
    count: 0,
    loading: false,
};

export const getPhotos = createAsyncThunk(
    'photos/getPhotos',
    async (filter: FilterDto) => {
        return await fetchPhotos(filter);
    }
);

const photoSlice = createSlice({
    name: 'photos',
    initialState,
    reducers: {},
    extraReducers: builder => {
        builder
            .addCase(getPhotos.pending, state => {
                state.loading = true;
            })
            .addCase(getPhotos.fulfilled, (state, action) => {
                state.items = action.payload.photos ?? [];
                state.count = action.payload.count;
                state.loading = false;
            })
            .addCase(getPhotos.rejected, state => {
                state.loading = false;
            });
    },
});

export default photoSlice.reducer;
