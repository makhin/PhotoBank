import { defineConfig } from 'vitest/config';
import path from 'path';

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
