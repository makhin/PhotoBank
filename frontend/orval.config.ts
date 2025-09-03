import { defineConfig } from 'orval';
import path from 'node:path';

const OPENAPI = path.resolve(__dirname, '../openapi.yaml');

export default defineConfig({
  frontend: {
    input: OPENAPI,
    output: {
      target: path.resolve(__dirname, 'packages/shared/src/api/photobank'),
      client: 'react-query',
      httpClient: 'fetch',
      mode: 'tags-split',
      mock: {
        type: 'msw',
        baseUrl: '/api',
      },
       override: {
          mutator: {
            path: path.resolve(__dirname, 'packages/shared/src/api/photobank/fetcher.ts'),
            name: 'customFetcher'
          },
         query: {
           useQuery: true,
           useInfinite: false,
         },
        },
    },
  },

  bot: {
    input: OPENAPI,
    output: {
      target: path.resolve(__dirname, 'packages/telegram-bot/src/api/photobank'),
      client: 'axios',
      mode: 'tags-split',
      mock: {
        type: 'msw',
        baseUrl: '/api',
      },
      override: {
        mutator: {
          path: path.resolve(__dirname, 'packages/telegram-bot/src/api/axios-instance.ts'),
          name: 'photobankAxios',
        },
      },
    },
  },
});
