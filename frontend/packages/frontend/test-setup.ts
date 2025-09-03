import { configureApi } from '@photobank/shared/api/photobank';
import { setupServer } from 'msw/node';
import type { HttpHandler } from 'msw';              // ← тип для handlers

import { handlers } from '@/mocks/handlers';

import '@testing-library/jest-dom/vitest';
import './src/shared/config/i18n';

configureApi('/api');

// ---- ResizeObserver без any
class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
Object.defineProperty(globalThis, 'ResizeObserver', {  // ← вместо (globalThis as any).ResizeObserver
  writable: true,
  value: ResizeObserver,
});

// ---- matchMedia стаб
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

// ---- IntersectionObserver стаб
Object.defineProperty(window, 'IntersectionObserver', {
  writable: true,
  value: class {
    observe() {}
    unobserve() {}
    disconnect() {}
    takeRecords() { return []; }
  },
});

// ---- Canvas стаб
Object.defineProperty(globalThis, 'HTMLCanvasElement', {
  writable: true,
  value: class {
    getContext(): CanvasRenderingContext2D | null { return null; }
  },
});

// ---- server без any
export const server = setupServer(...(handlers as HttpHandler[]));  // ← типизировано
