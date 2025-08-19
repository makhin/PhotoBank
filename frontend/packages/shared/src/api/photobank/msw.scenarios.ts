import { faker } from '@faker-js/faker';
import { http, HttpResponse, delay, type HttpHandler } from 'msw';

import { server } from '@/test-setup-server';

interface MakeUserOptions {
  role?: 'Admin' | 'User';
  email?: string | null;
  [key: string]: unknown;
}

export const makeUser = ({ role = 'User', ...rest }: MakeUserOptions = {}) => ({
  id: faker.string.uuid(),
  email: faker.internet.email(),
  roles: [role],
  ...rest,
});

interface DateRange {
  from: Date;
  to: Date;
}

interface MakePhotoOptions {
  tags?: number[];
  persons?: number[];
  storageId?: number;
  dateRange?: DateRange;
  [key: string]: unknown;
}

export const makePhoto = ({
  tags = [],
  persons = [],
  storageId = faker.number.int(),
  dateRange,
  ...rest
}: MakePhotoOptions = {}) => ({
  id: faker.number.int(),
  thumbnail: faker.image.url(),
  name: faker.word.words(2),
  takenDate: (
    dateRange
      ? faker.date.between({ from: dateRange.from, to: dateRange.to })
      : faker.date.recent()
  ).toISOString(),
  storageId,
  tags: tags.map((tagId) => ({ tagId })),
  persons: persons.map((personId) => ({ personId })),
  ...rest,
});

interface Range {
  from: string;
  to: string;
}

interface MakeAccessProfileOptions {
  storages?: number[];
  personGroups?: number[];
  dateRanges?: Range[];
  [key: string]: unknown;
}

export const makeAccessProfile = ({
  storages = [],
  personGroups = [],
  dateRanges = [],
  ...rest
}: MakeAccessProfileOptions = {}) => ({
  id: faker.number.int(),
  name: faker.word.noun(),
  storages: storages.map((storageId) => ({ storageId })),
  personGroups: personGroups.map((personGroupId) => ({ personGroupId })),
  dateRanges: dateRanges.map(({ from, to }) => ({ fromDate: from, toDate: to })),
  ...rest,
});

const createScenario = (...handlers: HttpHandler[]) => ({
  handlers,
  apply: () => server.use(...handlers),
});

const defaultPhotos = Array.from({ length: 3 }, () => makePhoto());

export const scenarioAdminAllAccess = createScenario(
  http.get('/api/auth/user', () => HttpResponse.json(makeUser({ role: 'Admin' }))),
  http.post('/api/photos/search', () =>
    HttpResponse.json({ totalCount: defaultPhotos.length, items: defaultPhotos }),
  ),
  http.get('/api/admin/access-profiles', () =>
    HttpResponse.json([makeAccessProfile()]),
  ),
);

export const scenarioUserLimitedAccess = createScenario(
  http.get('/api/auth/user', () => HttpResponse.json(makeUser({ role: 'User' }))),
  http.post('/api/photos/search', () =>
    HttpResponse.json({ totalCount: 1, items: [makePhoto()] }),
  ),
  http.get('/api/admin/access-profiles', () =>
    HttpResponse.json([makeAccessProfile({ storages: [1] })]),
  ),
);

export const scenarioEmptyResult = createScenario(
  http.post('/api/photos/search', () =>
    HttpResponse.json({ totalCount: 0, items: [] }),
  ),
);

export const scenarioError401 = createScenario(
  http.all('*', () => new HttpResponse(null, { status: 401 })),
);

export const scenarioSlowNetwork = createScenario(
  http.post('/api/photos/search', async () => {
    await delay(3000);
    return HttpResponse.json({ totalCount: defaultPhotos.length, items: defaultPhotos });
  }),
  http.get('/api/auth/user', async () => {
    await delay(3000);
    return HttpResponse.json(makeUser({ role: 'User' }));
  }),
);

