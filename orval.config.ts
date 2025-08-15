import { defineConfig } from 'orval';
export default defineConfig({
  frontend: {
    input: './openapi.yaml',
    output: {
      target: 'packages/shared/src/api-frontend.ts',
      client: 'react-query',
 mode: 'tags-split',
    },
  },
  bot: {
    input: './openapi.yaml',
    output: {
      target: 'packages/telegram-bot/src/api/generated.ts',
      client: 'axios',
 mode: 'tags-split',
      override: { mutator: { path: './axios-instance.ts', name: 'photobankAxios' } },
    },
  },
});