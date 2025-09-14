// packages/telegram-bot/tsup.config.ts
import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  target: 'node20',
  platform: 'node',
  format: ['esm'],         // выходим ESM'ом
  outDir: 'dist',
  sourcemap: true,
  clean: true,
  dts: false,
  splitting: false,        // один файл, без чанков
  treeshake: true,
  minify: false,

  // Ключевое: не тащим node_modules в бандл
  skipNodeModulesBundle: true,

  // Если есть внутренние workspace-пакеты, которые хочется «вплавить» — перечисли их тут.
  // Это безопасно. Внешние зависимости останутся внешними.
  noExternal: ['@photobank/shared'],

  // На всякий пожарный: не бандлим деношные шимы, если они вдруг объявлены как deps.
  external: ['@deno/shim-deno', '@deno/shim-deno-test'],
});