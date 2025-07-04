import { defineConfig } from 'vitest/config'
import path from 'path'

export default defineConfig({
  resolve: {
    alias: {
      '@photobank/shared': path.resolve(__dirname, './src'),
      '@photobank/shared/': path.resolve(__dirname, './src/')
    }
  },
  test: {
    environment: 'node'
  }
})
