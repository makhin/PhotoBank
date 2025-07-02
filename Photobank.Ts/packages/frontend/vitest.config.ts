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
    environment: 'jsdom'
  }
});
