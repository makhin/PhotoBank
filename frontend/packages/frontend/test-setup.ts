import { setupServer } from 'msw/node';
import { handlers } from './src/mocks/handlers';

import '@testing-library/jest-dom/vitest';
import './src/shared/config/i18n';

class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
(globalThis as any).ResizeObserver = ResizeObserver;

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    media: query,
    matches: false,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

Object.defineProperty(window, 'IntersectionObserver', {
  writable: true,
  value: class {
    observe() {}
    unobserve() {}
    disconnect() {}
    takeRecords() { return []; }
  },
});

Object.defineProperty(globalThis, 'HTMLCanvasElement', {
  writable: true,
  value: class {
    getContext() { return null; }
  },
});

export const server = setupServer(...(handlers as any));
beforeAll(() =>
  server.listen({
    onUnhandledRequest(request, print) {
      const url = new URL(request.url);
      if (url.pathname.startsWith('/assets')) {
        return;
      }

      print.warning();
    },
  }),
);
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
