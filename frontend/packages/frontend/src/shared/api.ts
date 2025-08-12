import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react';
import * as Api from '@photobank/shared/src/api/photobank';

import { orvalQuery, orvalMutation } from './orvalAdapter';

export const photobankApi = createApi({
  reducerPath: 'photobankApi',
  baseQuery: fakeBaseQuery<{ status: number; data?: unknown; problem?: unknown }>(),
  endpoints: (build) => ({
    login: build.mutation<Api.LoginResponseDto, Api.LoginRequestDto>({
      queryFn: orvalMutation((body, opt) => Api.authLogin(body, opt).then((r) => r.data)),
    }),
    getPhotoById: build.query<Api.PhotoDto, number>({
      queryFn: orvalQuery((id, opt) => Api.getApiPhotos(id, opt).then((r) => r.data)),
    }),
    // add other endpoints similarly
  }),
});

export const { useLoginMutation, useGetPhotoByIdQuery } = photobankApi;
