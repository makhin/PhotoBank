import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  outDir: 'dist',
  format: ['esm'],              // можно добавить 'cjs' если нужен dual build
  target: 'node20',
  splitting: false,
  sourcemap: true,
  clean: true,                  // очищает dist перед сборкой
  dts: true,                    // генерит index.d.ts
  external: ['grammy'],         // зависимости, которые не надо бандлить
  treeshake: true,
  skipNodeModulesBundle: true   // не пакует node_modules внутрь
});
