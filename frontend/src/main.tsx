import { ConfigProvider } from 'antd';
import ruRU from 'antd/locale/ru_RU';
import dayjs from 'dayjs';
import 'dayjs/locale/ru';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { AuthProvider } from 'react-oidc-context';
import App from './App';
import { oidcConfig } from './auth/authConfig';
import './index.css';

dayjs.locale('ru');

// Если Keycloak ещё не настроен — VITE_AUTH_DISABLED=true в .env
// позволяет открыть приложение без реальной авторизации (см. useAppAuth.ts).
const AUTH_DISABLED = import.meta.env.VITE_AUTH_DISABLED === 'true';

const appTree = (
  <ConfigProvider locale={ruRU}>
    <App />
  </ConfigProvider>
);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    {AUTH_DISABLED ? appTree : <AuthProvider {...oidcConfig}>{appTree}</AuthProvider>}
  </React.StrictMode>,
);
