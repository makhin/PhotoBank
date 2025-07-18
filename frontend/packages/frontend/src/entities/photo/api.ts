import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react';
import {
  searchPhotos as searchPhotosApi,
  getPhotoById as getPhotoByIdApi,
  updateFace as updateFaceApi,
} from '@photobank/shared/api';
import type { UpdateFaceDto } from '@photobank/shared/types';

import type { FilterDto, PhotoDto, QueryResult } from '@photobank/shared/types';

export const api = createApi({
  reducerPath: 'photobankApi',
  baseQuery: fakeBaseQuery(),
  endpoints: (builder) => ({
    getPhotoById: builder.query<PhotoDto, number>({
      async queryFn(id) {
        try {
          const data = await getPhotoByIdApi(id);
          return { data: data as PhotoDto };
        } catch (error) {
          return { error: error as unknown as Error };
        }
      },
    }),
    searchPhotos: builder.mutation<QueryResult, FilterDto>({
      async queryFn(filter) {
        try {
          const data = await searchPhotosApi(filter);
          return { data: data as QueryResult };
        } catch (error) {
          return { error: error as unknown as Error };
        }
      },
    }),
    updateFace: builder.mutation<void, UpdateFaceDto>({
      async queryFn(dto) {
        try {
          await updateFaceApi(dto);
          return { data: undefined };
        } catch (error) {
          return { error: error as unknown as Error };
        }
      },
    }),
  }),
});

export const {
  useGetPhotoByIdQuery,
  useSearchPhotosMutation,
  useUpdateFaceMutation,
} = api;
