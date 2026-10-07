import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  css: { preprocessorOptions: { scss: { silenceDeprecations: ['import', 'global-builtin', 'color-functions', 'if-function', 'abs-percent', 'function-units'] } } },
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
