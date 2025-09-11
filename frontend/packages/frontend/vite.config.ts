import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tsconfigPaths from 'vite-tsconfig-paths';

export default defineConfig({
  plugins: [react(), tsconfigPaths()],
  resolve: {
    preserveSymlinks: true
  },
  build: {
    target: 'es2022',
    sourcemap: true
  },
  optimizeDeps: {
    include: [
      '@tanstack/query-core',
      '@tanstack/react-query',
      'redux',
      'cookie',
      'use-sync-external-store/with-selector',
      'eventemitter3'
    ]
  }
});
