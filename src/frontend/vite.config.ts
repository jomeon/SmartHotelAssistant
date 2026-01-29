/// <reference types="vitest" />
import { defineConfig } from 'vitest/config' // <--- ZMIANA TUTAJ (zamiast 'vite')
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    
  },
})