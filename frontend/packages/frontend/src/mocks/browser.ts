import { setupWorker } from 'msw';
import { handlers as authHandlers } from '@photobank/shared/api/photobank/auth/auth.msw';
import { handlers as facesHandlers } from '@photobank/shared/api/photobank/faces/faces.msw';
import { handlers as photosHandlers } from '@photobank/shared/api/photobank/photos/photos.msw';
import { handlers as personsHandlers } from '@photobank/shared/api/photobank/persons/persons.msw';
import { handlers as tagsHandlers } from '@photobank/shared/api/photobank/tags/tags.msw';
import { handlers as usersHandlers } from '@photobank/shared/api/photobank/users/users.msw';
import { handlers as pathsHandlers } from '@photobank/shared/api/photobank/paths/paths.msw';
import { handlers as storagesHandlers } from '@photobank/shared/api/photobank/storages/storages.msw';

export const worker = setupWorker(
  ...authHandlers,
  ...facesHandlers,
  ...photosHandlers,
  ...personsHandlers,
  ...tagsHandlers,
  ...usersHandlers,
  ...pathsHandlers,
  ...storagesHandlers,
);
