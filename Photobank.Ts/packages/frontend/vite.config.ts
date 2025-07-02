import { defineConfig } from 'vite'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react(), tailwindcss(),],
    server: {
        host: '0.0.0.0',
        port: 5173
    },
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
            "@photobank/shared": path.resolve(__dirname, "../shared/src"),
        },
    },
})
