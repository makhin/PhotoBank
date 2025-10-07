import { defineConfig } from 'orval';
import path from 'node:path';

const OPENAPI = path.resolve(__dirname, '../openapi.yaml');

export default defineConfig({
  frontend: {
    input: OPENAPI,
    output: {
      target: path.resolve(__dirname, 'packages/shared/src/api/photobank'),
      client: 'react-query',
      prettier: true,
      httpClient: 'fetch',
      mode: 'tags-split',
       override: {
         useDates: true,
         mutator: {
            path: path.resolve(__dirname, 'packages/shared/src/api/photobank/fetcher.ts'),
            name: 'customFetcher'
          },
         query: {
           signal: false,
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
      client: 'fetch',
      mode: 'tags-split',
      prettier: true,
      override: {
        useDates: true,
        mutator: {
          path: path.resolve(__dirname, 'packages/shared/src/api/photobank/fetcher.ts'),
          name: 'customFetcher',
        },
      },
    },
  },
});
