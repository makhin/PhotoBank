import {createAsyncThunk, createSlice, type PayloadAction,} from '@reduxjs/toolkit';

import type {PathDto, PersonDto, StorageDto, TagDto,} from '@/entities/meta/model.ts';
import {BASE_URL, METADATA_CACHE_KEY, METADATA_CACHE_VERSION} from "@/shared/constants.ts";

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

    const [storages, tags, persons, paths] = await Promise.all([
        fetch(`${BASE_URL}/api/storages`).then((res) => res.json()) as Promise<StorageDto[]>,
        fetch(`${BASE_URL}/api/tags`).then((res) => res.json()) as Promise<TagDto[]>,
        fetch(`${BASE_URL}/api/persons`).then((res) => res.json()) as Promise<PersonDto[]>,
        fetch(`${BASE_URL}/api/paths`).then((res) => res.json()) as Promise<PathDto[]>,
    ]);

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
