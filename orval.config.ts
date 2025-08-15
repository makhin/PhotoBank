import { defineConfig } from 'orval';
export default defineConfig({
  frontend: {
    input: './openapi.yaml',
    output: {
      target: 'frontend/packages/shared/src/api/photobank',
      client: 'react-query',
 mode: 'tags-split',
mock:true,
    },
  },
  bot: {
    input: './openapi.yaml',
    output: {
      target: 'frontend/packages/telegram-bot/src/api/photobank',
      client: 'axios',
 mode: 'tags-split',
mock:true,
      override: { mutator: { path: 'frontend/packages/telegram-bot/src/api/axios-instance.ts', name: 'photobankAxios' } },
    },
  },
});