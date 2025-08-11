import { getAuthMock } from './auth/auth.msw';
import { getFacesMock } from './faces/faces.msw';
import { getPathsMock } from './paths/paths.msw';
import { getPersonsMock } from './persons/persons.msw';
import { getPhotosMock } from './photos/photos.msw';
import { getStoragesMock } from './storages/storages.msw';
import { getTagsMock } from './tags/tags.msw';
import { getUsersMock } from './users/users.msw';

export const getPhotobankMock = () => [
  ...getAuthMock(),
  ...getFacesMock(),
  ...getPathsMock(),
  ...getPersonsMock(),
  ...getPhotosMock(),
  ...getStoragesMock(),
  ...getTagsMock(),
  ...getUsersMock(),
];
