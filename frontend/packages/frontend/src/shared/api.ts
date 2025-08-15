import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react';
import * as Api from '@photobank/shared/api/photobank';
import { unwrapOrThrow } from './httpUtils';
import type { ProblemDetailsError } from '@photobank/shared/types/problem';

export const photobankApi = createApi({
  reducerPath: 'photobankApi',
  baseQuery: fakeBaseQuery<ProblemDetailsError>(),
  endpoints: (build) => ({
    login: build.mutation<Api.LoginResponseDto, Api.LoginRequestDto, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.authLogin(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getPhotoById: build.query<Api.PhotoDto, number, ProblemDetailsError>({
      queryFn: async (id, api) => {
        try {
          const data = await unwrapOrThrow(Api.getApiPhotos(id, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    register: build.mutation<null, Api.RegisterRequestDto, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.authRegister(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getUser: build.query<Api.UserDto, void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.authGetUser({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    updateUser: build.mutation<null, Api.UpdateUserDto, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.authUpdateUser(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getUserClaims: build.query<Api.ClaimDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.authGetUserClaims({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getUserRoles: build.query<Api.RoleDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.authGetUserRoles({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    searchPhotos: build.mutation<Api.PageResponseOfPhotoItemDto, Api.FilterDto, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.postApiPhotosSearch(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    uploadPhotos: build.mutation<null, Api.PhotosUploadBody, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.photosUpload(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getDuplicatePhotos: build.query<Api.PhotoItemDto[], Api.PhotosGetDuplicatesParams | void, ProblemDetailsError>({
      queryFn: async (params, api) => {
        try {
          const data = await unwrapOrThrow(Api.photosGetDuplicates(params, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getPersons: build.query<Api.PersonDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.personsGetAll({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    updateFace: build.mutation<null, Api.UpdateFaceDto, ProblemDetailsError>({
      queryFn: async (body, api) => {
        try {
          const data = await unwrapOrThrow(Api.facesUpdate(body, { signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getAdminUsers: build.query<Api.UserWithClaimsDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.usersGetAll({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    updateAdminUser: build.mutation<null, { id: string; data: Api.UpdateUserDto }, ProblemDetailsError>({
      queryFn: async ({ id, data }, api) => {
        try {
          const res = await unwrapOrThrow(Api.usersUpdate(id, data, { signal: api.signal }));
          return { data: res };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    setUserClaims: build.mutation<null, { id: string; data: Api.ClaimDto[] }, ProblemDetailsError>({
      queryFn: async ({ id, data }, api) => {
          try {
            const res = await unwrapOrThrow(Api.usersSetClaims(id, data, { signal: api.signal }));
            return { data: res };
          } catch (err) {
            return { error: err as ProblemDetailsError };
          }
      },
    }),
    getStorages: build.query<Api.StorageDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.storagesGetAll({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getTags: build.query<Api.TagDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.tagsGetAll({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
    getPaths: build.query<Api.PathDto[], void, ProblemDetailsError>({
      queryFn: async (_arg, api) => {
        try {
          const data = await unwrapOrThrow(Api.pathsGetAll({ signal: api.signal }));
          return { data };
        } catch (err) {
          return { error: err as ProblemDetailsError };
        }
      },
    }),
  }),
});

export const {
  useLoginMutation,
  useGetPhotoByIdQuery,
  useRegisterMutation,
  useGetUserQuery,
  useUpdateUserMutation,
  useGetUserClaimsQuery,
  useGetUserRolesQuery,
  useSearchPhotosMutation,
  useUploadPhotosMutation,
  useGetDuplicatePhotosQuery,
  useGetPersonsQuery,
  useUpdateFaceMutation,
  useGetAdminUsersQuery,
  useUpdateAdminUserMutation,
  useSetUserClaimsMutation,
  useGetStoragesQuery,
  useGetTagsQuery,
  useGetPathsQuery,
} = photobankApi;
