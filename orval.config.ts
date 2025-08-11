import { defineConfig } from 'orval';

export default defineConfig({
  photobank: {
    input: './openapi.yaml',
    output: {
      target: './frontend/packages/shared/src/api/photobank/index.ts',
      schemas: './frontend/packages/shared/src/api/photobank/model',
      mode: 'tags-split',
      mock: true,
      client: 'fetch',
      override: {
        mutator: {
          path: './frontend/packages/shared/src/api/photobank/fetcher.ts',
          name: 'customFetcher',
        },
      },
    },
  },
});
