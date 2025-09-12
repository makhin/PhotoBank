import { defineConfig } from 'vitest/config';
import * as path from 'node:path';

import '@testing-library/jest-dom/vitest';

// Моки для JSDOM
class ResizeObserver { observe(){} unobserve(){} disconnect(){} }
(globalThis as any).ResizeObserver = ResizeObserver;

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (q: string) => ({
    media: q, matches: false, onchange: null,
    addListener() {}, removeListener() {}, addEventListener() {}, removeEventListener() {},
    dispatchEvent() { return false; }
  }),
});

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@photobank/shared': path.resolve(__dirname, '../shared/src'),
      '@photobank/shared/': path.resolve(__dirname, '../shared/src/')
    }
  },
  test: {
    environment: 'jsdom',
    setupFiles: './test-setup.ts',
    globals: true,
    css: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html']
    }
  }
});
