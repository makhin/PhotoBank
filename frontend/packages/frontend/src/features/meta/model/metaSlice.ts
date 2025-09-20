import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import * as Api from '@photobank/shared/api/photobank';
import { fetchReferenceData } from '@photobank/shared/api/photobank';
import type { PathDto, PersonDto, StorageDto, TagDto } from '@photobank/shared/api/photobank';
import {
  METADATA_CACHE_KEY,
  METADATA_CACHE_VERSION,
} from '@photobank/shared/constants';
import { namespacedStorage } from '@photobank/shared/safeStorage';


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

const store = namespacedStorage('meta');

const loadFromCache = (): MetadataPayload | null => {
    const cached = store.get<MetadataPayload>(METADATA_CACHE_KEY);
    if (!cached) return null;
    if (cached.version !== METADATA_CACHE_VERSION) {
        store.remove(METADATA_CACHE_KEY);
        return null;
    }
    const { tags, persons, paths, storages } = cached;
    if (
        !Array.isArray(tags) ||
        !Array.isArray(persons) ||
        !Array.isArray(paths) ||
        !Array.isArray(storages)
    ) {
        return null;
    }
    return cached;
};

const saveToCache = (data: MetadataPayload) => {
    if (data.version !== METADATA_CACHE_VERSION) {
        store.remove(METADATA_CACHE_KEY);
        return;
    }
    store.set(METADATA_CACHE_KEY, data);
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

export const loadMetadata = createAsyncThunk('metadata/load', async (_, { signal }) => {
    const fromCache = loadFromCache();
    if (fromCache) return fromCache;

    const { tags, persons, paths, storages } = await fetchReferenceData({
        fetchTags: () => Api.tagsGetAll({ signal }).then((response) => response.data),
        fetchPersons: () => Api.personsGetAll({ signal }).then((response) => response.data),
        fetchStorages: () => Api.storagesGetAll({ signal }).then((response) => response.data),
        fetchPaths: () => Api.pathsGetAll({ signal }).then((response) => response.data),
    });

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
            store.remove(METADATA_CACHE_KEY);
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
                state.loaded = true;
                state.error = action.error.message ?? 'Unknown error';
            });
    },
});

export const {clearCache} = metadataSlice.actions;
export default metadataSlice.reducer;
