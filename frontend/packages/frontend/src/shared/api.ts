import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react';
import * as Api from '@photobank/shared/api/photobank';

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
    register: build.mutation<null, Api.RegisterRequestDto>({
      queryFn: orvalMutation((body, opt) => Api.authRegister(body, opt).then((r) => r.data)),
    }),
    getUser: build.query<Api.UserDto, void>({
      queryFn: orvalQuery((_arg, opt) => Api.authGetUser(opt).then((r) => r.data)),
    }),
    updateUser: build.mutation<null, Api.UpdateUserDto>({
      queryFn: orvalMutation((body, opt) => Api.authUpdateUser(body, opt).then((r) => r.data)),
    }),
    getUserClaims: build.query<Api.ClaimDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.authGetUserClaims(opt).then((r) => r.data)),
    }),
    getUserRoles: build.query<Api.RoleDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.authGetUserRoles(opt).then((r) => r.data)),
    }),
    searchPhotos: build.mutation<Api.QueryResult, Api.FilterDto>({
      queryFn: orvalMutation((body, opt) => Api.postApiPhotosSearch(body, opt).then((r) => r.data)),
    }),
    uploadPhotos: build.mutation<null, Api.PhotosUploadBody>({
      queryFn: orvalMutation((body, opt) => Api.photosUpload(body, opt).then((r) => r.data)),
    }),
    getDuplicatePhotos: build.query<Api.PhotoItemDto[], Api.PhotosGetDuplicatesParams | void>({
      queryFn: orvalQuery((params, opt) => Api.photosGetDuplicates(params, opt).then((r) => r.data)),
    }),
    getPersons: build.query<Api.PersonDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.personsGetAll(opt).then((r) => r.data)),
    }),
    updateFace: build.mutation<null, Api.UpdateFaceDto>({
      queryFn: orvalMutation((body, opt) => Api.facesUpdate(body, opt).then((r) => r.data)),
    }),
    getAdminUsers: build.query<Api.UserWithClaimsDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.usersGetAll(opt).then((r) => r.data)),
    }),
    updateAdminUser: build.mutation<null, { id: string; data: Api.UpdateUserDto }>({
      queryFn: orvalMutation(({ id, data }, opt) => Api.usersUpdate(id, data, opt).then((r) => r.data)),
    }),
    setUserClaims: build.mutation<null, { id: string; data: Api.ClaimDto[] }>({
      queryFn: orvalMutation(({ id, data }, opt) => Api.usersSetClaims(id, data, opt).then((r) => r.data)),
    }),
    getStorages: build.query<Api.StorageDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.storagesGetAll(opt).then((r) => r.data)),
    }),
    getTags: build.query<Api.TagDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.tagsGetAll(opt).then((r) => r.data)),
    }),
    getPaths: build.query<Api.PathDto[], void>({
      queryFn: orvalQuery((_arg, opt) => Api.pathsGetAll(opt).then((r) => r.data)),
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
