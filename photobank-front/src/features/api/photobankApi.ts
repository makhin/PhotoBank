import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
    FilterDto,
    QueryResult,
    PhotoDto,
} from '@/types/dto'

export const photobankApi = createApi({
    reducerPath: 'photobankApi',
    baseQuery: fetchBaseQuery({ baseUrl: '/api' }),
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
} = photobankApi
