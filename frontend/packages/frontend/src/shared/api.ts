import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react';
import * as Api from '@photobank/shared/src/api/photobank';
import type { LoginRequestDto, LoginResponseDto } from '@photobank/shared/types';

export const photobankApi = createApi({
  reducerPath: 'photobankApi',
  baseQuery: fakeBaseQuery<{ status: number; data?: unknown; problem?: unknown }>(),
  endpoints: (build) => ({
    login: build.mutation<LoginResponseDto, LoginRequestDto>({
      async queryFn(body) {
        try {
          const data = await Api.postApiAuthLogin(body);
          return { data };
        } catch (e: any) {
          return { error: { status: e.status ?? 500, problem: e.problem, data: undefined } };
        }
      },
    }),
    getPhotoById: build.query<Api.PhotoDto, string>({
      async queryFn(id) {
        try {
          const data = await Api.getApiPhotosById({ id });
          return { data };
        } catch (e: any) {
          return { error: { status: e.status ?? 500, problem: e.problem } };
        }
      },
    }),
    // add other endpoints similarly
  }),
});

export const { useLoginMutation, useGetPhotoByIdQuery } = photobankApi;
