// orval.config.ts
import { defineConfig } from 'orval';

export default defineConfig({
  frontend: {
    input: './openapi.yaml',
    output: {
      target: 'frontend/packages/shared/src/api/photobank', // директория (tags-split)
      client: 'react-query',
      httpClient: 'fetch',            // <-- используем fetch вместо axios
      mode: 'tags-split',
      mock: {
        generator: 'msw',
        baseUrl: '/api',
      },
      // Если нужен единый baseUrl для fetch, можно добавить override.mutator с кастомным fetch
      // override: { mutator: { path: 'frontend/packages/shared/src/api/fetcher.ts', name: 'fetcher' } },
    },
  },

  bot: {
    input: './openapi.yaml',
    output: {
      target: 'frontend/packages/telegram-bot/src/api/photobank',
      client: 'axios',                // бот остаётся на axios
      mode: 'tags-split',
      mock: {
        generator: 'msw',
        baseUrl: '/api',
      },
      override: {
        mutator: {
          path: 'frontend/packages/telegram-bot/src/api/axios-instance.ts',
          name: 'photobankAxios',
        },
      },
    },
  },
});