import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tsconfigPaths from 'vite-tsconfig-paths';
import * as path from 'node:path';

export default defineConfig({
  plugins: [
    react(),
    tsconfigPaths({
      // читаем только фронтовый tsconfig, игнорим ошибки в остальных пакетах
      projects: [path.resolve(__dirname, 'tsconfig.app.json')],
      ignoreConfigErrors: true,
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: { target: 'es2022', sourcemap: true },
  optimizeDeps: { entries: ['src/main.tsx'] },
});
