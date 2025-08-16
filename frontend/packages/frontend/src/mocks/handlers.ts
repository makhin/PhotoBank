import { getAuthMock } from '@photobank/shared/api/photobank/auth/auth.msw';
import { getFacesMock } from '@photobank/shared/api/photobank/faces/faces.msw';
import { getPhotosMock } from '@photobank/shared/api/photobank/photos/photos.msw';
import { getPersonsMock } from '@photobank/shared/api/photobank/persons/persons.msw';
import { getTagsMock } from '@photobank/shared/api/photobank/tags/tags.msw';
import { getUsersMock } from '@photobank/shared/api/photobank/users/users.msw';
import { getPathsMock } from '@photobank/shared/api/photobank/paths/paths.msw';
import { getStoragesMock } from '@photobank/shared/api/photobank/storages/storages.msw';

export const handlers = [
  ...getAuthMock(),
  ...getFacesMock(),
  ...getPhotosMock(),
  ...getPersonsMock(),
  ...getTagsMock(),
  ...getUsersMock(),
  ...getPathsMock(),
  ...getStoragesMock(),
];
