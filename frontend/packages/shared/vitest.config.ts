import path from 'path';
import { defineConfig, mergeConfig } from 'vitest/config';

import baseConfig from '../vitest.base';

export default mergeConfig(
  baseConfig,
  defineConfig({
    resolve: {
      alias: {
        '@': path.resolve(__dirname),
      },
    },
    test: {
      setupFiles: './test-setup.ts',
    },
  }),
);

