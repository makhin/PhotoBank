import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  target: 'node20',
  format: ['esm'],
  sourcemap: true,
  clean: true,
  dts: false,
  splitting: false,
  outDir: 'dist',
  noExternal: [/.*/],    // 🔥 бандлим ВСЕ зависимости, включая транзитивные (openai и ко)
  platform: 'node',
  treeshake: true,
});
