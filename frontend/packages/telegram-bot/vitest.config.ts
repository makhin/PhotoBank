import { defineConfig, mergeConfig } from 'vitest/config';
import baseConfig from '../vitest.base';

process.env.BOT_TOKEN ??= 'test-token';
process.env.API_BASE_URL ??= 'http://localhost';

export default mergeConfig(
  baseConfig,
  defineConfig({
    test: {
      setupFiles: './test-setup.ts',
    },
  }),
);
