import { afterAll, afterEach, beforeAll } from 'vitest';
import { setupServer } from 'msw/node';
let handlers: any[] = [];
try {
  const module = await import('@photobank/shared/api/photobank/msw');
  handlers = Array.isArray(module.handlers) ? module.handlers : [];
} catch {
  // shared MSW handlers are optional for lightweight tests
}

process.env.BOT_TOKEN = process.env.BOT_TOKEN || 'test-token';

export const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'bypass' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
