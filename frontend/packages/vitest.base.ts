import { defineConfig } from 'vitest/config';
import path from 'path';

const alias = {
  '@photobank/shared': path.resolve(__dirname, './shared/src'),
  '@photobank/shared/': path.resolve(__dirname, './shared/src/'),
};

export default defineConfig({
  resolve: { alias },
  test: { environment: 'node' },
});
