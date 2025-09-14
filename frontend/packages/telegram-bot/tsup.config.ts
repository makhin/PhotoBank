import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  outDir: 'dist',
  format: ['esm'],              // можно добавить 'cjs' если нужен dual build
  target: 'node20',
  splitting: false,
  sourcemap: true,
  clean: true,                  // очищает dist перед сборкой
  dts: false,                   // не генерит index.d.ts
  noExternal: ['@photobank/shared'], // <- ВАЖНО: не внешне, а внутрь бандла
  external: ['grammy'],         // зависимости, которые не надо бандлить
  treeshake: true,
  skipNodeModulesBundle: true   // не пакует node_modules внутрь
});
