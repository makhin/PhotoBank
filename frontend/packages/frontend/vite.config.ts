import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tsconfigPaths from 'vite-tsconfig-paths';
import * as path from 'node:path';

export default defineConfig({
  plugins: [react(), tsconfigPaths()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@photobank/shared': path.resolve(__dirname, '../../shared/src') // dev: брать исходники
    }
  },
  build: {
    target: 'es2022',
    sourcemap: true
  },
  optimizeDeps: {
    entries: ['src/main.tsx']
  }
});
