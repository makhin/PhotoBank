import { defineConfig } from 'vitest/config';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import tsconfigPaths from 'vite-tsconfig-paths';

const __dirname = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [
    tsconfigPaths({
      projects: [
        resolve(__dirname, './tsconfig.json'),
        resolve(__dirname, '../shared/tsconfig.json'),
      ],
    }),
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      '@photobank/shared': resolve(__dirname, '../shared/src'),
      '@photobank/shared/api/photobank/msw': resolve(
        __dirname,
        '../shared/src/api/photobank/msw.ts',
      ),
    },
  },
  test: {
    setupFiles: ['./test-setup.ts'],
    environment: 'node',
    restoreMocks: true,
  },
});
