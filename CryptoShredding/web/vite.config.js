import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api/customers': 'http://localhost:5231',
      '/api/orders': 'http://localhost:5122',
    },
  },
})
