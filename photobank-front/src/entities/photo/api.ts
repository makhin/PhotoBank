import {createApi, fetchBaseQuery} from '@reduxjs/toolkit/query/react'
import type {FilterDto, PhotoDto, QueryResult,} from '@/entities/meta/model.ts'

export const api = createApi({
    reducerPath: 'photobankApi',
    baseQuery: fetchBaseQuery({baseUrl: '/api'}),
    endpoints: (builder) => ({
        getPhotoById: builder.query<PhotoDto, number>({
            query: (id) => `photos/${id}`,
        }),
        searchPhotos: builder.mutation<QueryResult, FilterDto>({
            query: (filter) => ({
                url: 'photos/search',
                method: 'POST',
                body: filter,
            }),
        }),
    }),
})

export const {
    useGetPhotoByIdQuery,
    useSearchPhotosMutation,
} = api
