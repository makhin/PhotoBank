import path from 'path';
import { defineConfig, mergeConfig } from 'vitest/config';

import baseConfig from '../vitest.base';

export default mergeConfig(
  baseConfig,
  defineConfig({
    resolve: {
      alias: {
        'react-native': path.resolve(__dirname, './react-native-stub.ts'),
      },
    },
  })
);
