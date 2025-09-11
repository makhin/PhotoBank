import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  format: ['esm'],        // единый ESM
  platform: 'node',
  target: 'es2022',
  bundle: true,           // ВАЖНО: бандлим, чтобы не осталось относительных импортов
  splitting: false,
  sourcemap: true,
  clean: true,
  dts: false,
  outDir: 'dist',
  // для дев-режима удобен watch + onSuccess (см. scripts)
});
