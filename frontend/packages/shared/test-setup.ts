import { setupServer } from 'msw/node';
import { faker } from '@faker-js/faker';
import { afterAll, afterEach, beforeAll } from 'vitest';

import { getPhotobankMock } from './src/api/photobank/msw';

export const server = setupServer(...getPhotobankMock());

beforeAll(() => {
  faker.seed(42);
  server.listen({ onUnhandledRequest: 'bypass' });
});

afterEach(() => server.resetHandlers());
afterAll(() => server.close());

