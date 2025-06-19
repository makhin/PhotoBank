import {createAsyncThunk, createSlice, type PayloadAction} from '@reduxjs/toolkit'

import type {PathDto, PersonDto, StorageDto, TagDto,} from '@/types/dto'

interface MetadataPayload {
    tags: TagDto[]
    persons: PersonDto[]
    paths: PathDto[]
    storages: StorageDto[]
    version: number
}

interface MetadataState extends Omit<MetadataPayload, 'version'> {
    version: number
    loaded: boolean
    loading: boolean
    error?: string
}

const LOCAL_KEY = 'photobank_metadata_cache'
const LOCAL_VERSION = 1

const loadFromCache = (): MetadataPayload | null => {
    try {
        const raw = localStorage.getItem(LOCAL_KEY)
        if (!raw) return null
        const parsed = JSON.parse(raw)
        return parsed.version === LOCAL_VERSION ? parsed : null
    } catch {
        return null
    }
}

const saveToCache = (data: MetadataPayload) => {
    try {
        localStorage.setItem(LOCAL_KEY, JSON.stringify(data))
    } catch {
        console.error('saveToCache error')
    }
}

const initialState: MetadataState = {
    tags: [],
    persons: [],
    paths: [],
    storages: [],
    version: LOCAL_VERSION,
    loaded: false,
    loading: false,
    error: undefined
}

export const loadMetadata = createAsyncThunk(
    'metadata/load',
    async () => {
        const fromCache = loadFromCache()
        if (fromCache) return fromCache

        const [tags, persons, paths, storages] = await Promise.all([
            fetch('/api/storages').then(res => res.json()),
            fetch('/api/tags').then(res => res.json()),
            fetch('/api/persons').then(res => res.json()),
            fetch('/api/paths').then(res => res.json()),
        ])

        const result: MetadataPayload = {tags, persons, paths, storages, version: LOCAL_VERSION}
        saveToCache(result)
        return result
    }
)

export const metadataSlice = createSlice({
    name: 'metadata',
    initialState,
    reducers: {
        clearCache(state) {
            localStorage.removeItem(LOCAL_KEY)
            state.loaded = false
        }
    },
    extraReducers: builder => {
        builder
            .addCase(loadMetadata.pending, state => {
                state.loading = true
                state.error = undefined
            })
            .addCase(loadMetadata.fulfilled, (state, action: PayloadAction<MetadataPayload>) => {
                Object.assign(state, {
                    tags: action.payload.tags,
                    persons: action.payload.persons,
                    paths: action.payload.paths,
                    storages: action.payload.storages,
                    version: action.payload.version,
                    loading: false,
                    loaded: true
                })
            })
            .addCase(loadMetadata.rejected, (state, action) => {
                state.loading = false
                state.error = action.error.message
            })
    }
})

export const {clearCache} = metadataSlice.actions
export default metadataSlice.reducer
