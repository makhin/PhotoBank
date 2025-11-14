import { defineConfig } from 'orval';
import path from 'node:path';

const OPENAPI = path.resolve(__dirname, '../openapi.yaml');

export default defineConfig({
  // Генерация для web-приложения (frontend) с fetch client
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
        useTypeOverInterfaces: true,
        useUnionTypes: true,
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

  // Генерация для TV приложения (React Native) с axios client
  tv: {
    input: OPENAPI,
    output: {
      target: path.resolve(__dirname, 'packages/tv/src/api/generated'),
      client: 'react-query',
      prettier: true,
      mode: 'tags-split',
      baseUrl: '/api',
      clean: true,
      override: {
        useDates: true,
        useTypeOverInterfaces: true,
        useUnionTypes: true,
        mutator: {
          path: path.resolve(__dirname, 'packages/tv/src/api/client.ts'),
          name: 'customInstance',
        },
        query: {
          useQuery: true,
          useMutation: true,
          signal: true,
        },
      },
    },
    hooks: {
      afterAllFilesWrite: 'prettier --write',
    },
  },
});
