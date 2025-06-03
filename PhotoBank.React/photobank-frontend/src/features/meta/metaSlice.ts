import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import {
    fetchTags,
    fetchPersons,
    fetchStorages,
    fetchPaths,
} from '@/services/photoApi.ts';
import type {TagDto, PersonDto, StorageDto, PathDto} from '@/types/api';

interface MetaState {
    tags: TagDto[];
    persons: PersonDto[];
    storages: StorageDto[];
    paths: PathDto[];
}

const initialState: MetaState = {
    tags: [],
    persons: [],
    storages: [],
    paths: [],
};

export const getTags = createAsyncThunk('meta/getTags', fetchTags);
export const getPersons = createAsyncThunk('meta/getPersons', fetchPersons);
export const getStorages = createAsyncThunk('meta/getStorages', fetchStorages);
export const getPaths = createAsyncThunk('meta/getPaths', fetchPaths);

const metaSlice = createSlice({
    name: 'meta',
    initialState,
    reducers: {},
    extraReducers: builder => {
        builder
            .addCase(getTags.fulfilled, (state, action) => {
                state.tags = action.payload;
            })
            .addCase(getPersons.fulfilled, (state, action) => {
                state.persons = action.payload;
            })
            .addCase(getStorages.fulfilled, (state, action) => {
                state.storages = action.payload;
            })
            .addCase(getPaths.fulfilled, (state, action) => {
                state.paths = action.payload;
            });
    },
});

export default metaSlice.reducer;
