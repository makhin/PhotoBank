// src/features/meta/metaSlice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import type {
    StorageDto,
    TagDto,
    PersonDto,
    PathDto,
} from '@/types/dto'

export const loadMetaData = createAsyncThunk('meta/load', async () => {
    const [storages, tags, persons, paths] = await Promise.all([
        fetch('/api/storages').then(res => res.json()),
        fetch('/api/tags').then(res => res.json()),
        fetch('/api/persons').then(res => res.json()),
        fetch('/api/paths').then(res => res.json()),
    ])
    return { storages, tags, persons, paths }
})

interface MetaState {
    storages: StorageDto[]
    tags: TagDto[]
    persons: PersonDto[]
    paths: PathDto[]
    loaded: boolean
    loading: boolean
    error?: string
}

const initialState: MetaState = {
    storages: [],
    tags: [],
    persons: [],
    paths: [],
    loaded: false,
    loading: false,
}

const metaSlice = createSlice({
    name: 'meta',
    initialState,
    reducers: {},
    extraReducers: (builder) => {
        builder
            .addCase(loadMetaData.pending, (state) => {
                state.loading = true
                state.error = undefined
            })
            .addCase(loadMetaData.fulfilled, (state, action) => {
                state.storages = action.payload.storages
                state.tags = action.payload.tags
                state.persons = action.payload.persons
                state.paths = action.payload.paths
                state.loaded = true
                state.loading = false
            })
            .addCase(loadMetaData.rejected, (state, action) => {
                state.loading = false
                state.error = action.error.message
            })
    },
})

export default metaSlice.reducer
