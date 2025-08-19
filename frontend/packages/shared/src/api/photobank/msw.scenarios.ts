import { http } from 'msw';
import { respond, respondError, withDelay } from './msw.helpers';
import { handlers as baseHandlers } from './msw';

// If you already centralize API base url in a config, import it instead
const API = (path: string) => `/api${path}`; // adjust if you prefix with /photobank

// Minimal deterministic factories. Replace with your DTO types from @photobank/shared/types
export type Role = 'Admin' | 'User';

export const makeUser = (overrides?: Partial<{ id: string; email: string; role: Role }>) => ({
  id: 'u_1',
  email: 'user@example.com',
  role: 'User' as Role,
  ...overrides,
});

export const makePhoto = (
  overrides?: Partial<{
    id: string;
    storageId: string;
    tags: string[];
    persons: string[];
    date: string;
  }>,
) => ({
  id: 'p_1',
  storageId: 'st_1',
  tags: ['summer', 'sea'],
  persons: ['per_1'],
  date: '2024-08-01T12:00:00Z',
  ...overrides,
});

export const makeAccessProfile = (
  overrides?: Partial<{
    storages: string[];
    personGroups: string[];
    dateRanges: Array<{ from: string; to: string }>;
  }>,
) => ({
  storages: ['st_1'],
  personGroups: ['grp_1'],
  dateRanges: [{ from: '2024-01-01', to: '2024-12-31' }],
  ...overrides,
});

/** Compose scenario-specific handlers on top of base orval handlers */
export const scenarioAdminAllAccess = () => [
  ...baseHandlers,
  http.get(API('/auth/me'), async () => {
    await withDelay(80);
    return respond(makeUser({ role: 'Admin' }));
  }),
  http.get(API('/photos'), async () => {
    await withDelay(120);
    return respond({
      items: [
        makePhoto({ id: 'p_1' }),
        makePhoto({ id: 'p_2', persons: ['per_1', 'per_2'], tags: ['family'] }),
      ],
      total: 2,
    });
  }),
];

export const scenarioUserLimitedAccess = () => [
  ...baseHandlers,
  http.get(API('/auth/me'), async () => respond(makeUser({ role: 'User' }))),
  http.get(API('/access/profile'), async () => respond(makeAccessProfile())),
  http.get(API('/photos'), async ({ request }) => {
    // Here you can inspect search params to filter deterministically
    const url = new URL(request.url);
    const storageFilter = url.searchParams.get('storageId');

    const allowedStorage = 'st_1';
    const items = [
      makePhoto({ id: 'p_1', storageId: allowedStorage, persons: [], tags: ['ok'] }),
      makePhoto({ id: 'p_2', storageId: allowedStorage, persons: ['per_1'] }),
      // This one must be filtered-out in the app logic if storage mismatch
      makePhoto({ id: 'p_3', storageId: 'st_2', persons: ['per_9'] }),
    ];

    const filtered = items.filter((p) => !storageFilter || p.storageId === storageFilter);
    return respond({ items: filtered, total: filtered.length });
  }),
];

export const scenarioError401 = () => [
  ...baseHandlers,
  http.get(API('/photos'), async () => respondError(401, 'Unauthorized')),
  http.get(API('/auth/me'), async () => respondError(401, 'Unauthorized')),
];

export const scenarioSlowNetwork = (ms = 1500) => [
  ...baseHandlers,
  http.get(API('/photos'), async () => {
    await withDelay(ms);
    return respond({ items: [], total: 0 });
  }),
];

