import '@testing-library/jest-dom/vitest';

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
