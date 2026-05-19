import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    host: "0.0.0.0",
    port: 5173,
    strictPort: true
  },
  resolve: {
    // Keep symlinked workspace paths stable in this environment.
    preserveSymlinks: true
  }
})
