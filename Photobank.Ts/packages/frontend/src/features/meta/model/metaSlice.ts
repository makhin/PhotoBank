import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { getAllPaths, getAllPersons, getAllStorages, getAllTags } from '@photobank/shared/api';
import type { PathDto, PersonDto, StorageDto, TagDto } from '@photobank/shared/types';

import {
  METADATA_CACHE_KEY,
  METADATA_CACHE_VERSION,
} from '@/shared/constants';

interface MetadataPayload {
    tags: TagDto[];
    persons: PersonDto[];
    paths: PathDto[];
    storages: StorageDto[];
    version: number;
}

interface MetadataState extends Omit<MetadataPayload, 'version'> {
    version: number;
    loaded: boolean;
    loading: boolean;
    error?: string;
}

const loadFromCache = (): MetadataPayload | null => {
    try {
        const raw = localStorage.getItem(METADATA_CACHE_KEY);
        if (!raw) return null;
        const parsed: MetadataPayload = JSON.parse(raw) as MetadataPayload;
        return parsed.version === METADATA_CACHE_VERSION ? parsed : null;
    } catch {
        return null;
    }
};

const saveToCache = (data: MetadataPayload) => {
    try {
        localStorage.setItem(METADATA_CACHE_KEY, JSON.stringify(data));
    } catch {
        console.error('saveToCache error');
    }
};

const initialState: MetadataState = {
    tags: [],
    persons: [],
    paths: [],
    storages: [],
    version: METADATA_CACHE_VERSION,
    loaded: false,
    loading: false,
    error: undefined,
};

export const loadMetadata = createAsyncThunk('metadata/load', async () => {
    const fromCache = loadFromCache();
    if (fromCache) return fromCache;

    const storages: StorageDto[] = await getAllStorages();
    const tags: TagDto[] = await getAllTags();
    const persons: PersonDto[] = await getAllPersons();
    const paths: PathDto[] = await getAllPaths();

    const result: MetadataPayload = {
        tags,
        persons,
        paths,
        storages,
        version: METADATA_CACHE_VERSION,
    };
    saveToCache(result);
    return result;
});

export const metadataSlice = createSlice({
    name: 'metadata',
    initialState,
    reducers: {
        clearCache(state) {
            localStorage.removeItem(METADATA_CACHE_KEY);
            state.loaded = false;
        },
    },
    extraReducers: (builder) => {
        builder
            .addCase(loadMetadata.pending, (state) => {
                state.loading = true;
                state.error = undefined;
            })
            .addCase(
                loadMetadata.fulfilled,
                (state, action: PayloadAction<MetadataPayload>) => {
                    Object.assign(state, {
                        tags: action.payload.tags,
                        persons: action.payload.persons,
                        paths: action.payload.paths,
                        storages: action.payload.storages,
                        version: action.payload.version,
                        loading: false,
                        loaded: true,
                    });
                }
            )
            .addCase(loadMetadata.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message;
            });
    },
});

export const {clearCache} = metadataSlice.actions;
export default metadataSlice.reducer;
