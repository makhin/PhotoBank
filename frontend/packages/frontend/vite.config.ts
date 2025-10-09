import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tsconfigPaths from 'vite-tsconfig-paths';
import * as path from 'node:path';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig(({ mode }) => {
  const workspaceRoot = path.resolve(__dirname, '../../');
  const sharedEnv = loadEnv(mode, workspaceRoot, 'VITE_');

  return {
    envDir: __dirname,
    define: {
      __APP_ENV__: sharedEnv,
    },
    plugins: [
      react(),
      tailwindcss(),
      tsconfigPaths({
        // читаем только фронтовый tsconfig, игнорим ошибки в остальных пакетах
        projects: [path.resolve(__dirname, 'tsconfig.app.json')],
        ignoreConfigErrors: true,
      }),
    ],
    server: { port: 5173, host: '0.0.0.0' },
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    build: { target: 'es2022', sourcemap: true },
    optimizeDeps: { entries: ['src/main.tsx'] },
  };
});
