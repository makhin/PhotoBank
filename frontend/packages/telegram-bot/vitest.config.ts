import { defineConfig } from 'vitest/config';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  resolve: {
    alias: {
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
