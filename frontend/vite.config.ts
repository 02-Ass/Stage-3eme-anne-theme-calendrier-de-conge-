import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Разрешает открывать dev-сервер не только с localhost — пригодилось,
    // когда пробовали пустить друга через туннель (ngrok/localtunnel).
    // Для обычной локальной разработки не мешает.
    allowedHosts: true,
  },
  test: {
    environment: 'node',
  },
});
