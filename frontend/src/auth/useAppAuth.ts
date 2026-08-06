import { useAuth as useOidcAuth } from 'react-oidc-context';

const AUTH_DISABLED = import.meta.env.VITE_AUTH_DISABLED === 'true';

/** Минимальный набор полей из react-oidc-context, которые реально использует App.tsx. */
export interface AppAuthState {
  isLoading: boolean;
  isAuthenticated: boolean;
  error?: Error;
  user?: {
    access_token?: string;
    profile: {
      preferred_username?: string;
      email?: string;
    };
  };
  signinRedirect: () => Promise<void> | void;
  signoutRedirect: (args?: { post_logout_redirect_uri?: string }) => Promise<void> | void;
}

const mockAuthState: AppAuthState = {
  isLoading: false,
  isAuthenticated: true,
  error: undefined,
  user: {
    access_token: undefined,
    profile: { preferred_username: 'локальная разработка (Keycloak отключён)' },
  },
  signinRedirect: () => {},
  signoutRedirect: () => {},
};

function useRealAuth(): AppAuthState {
  return useOidcAuth() as unknown as AppAuthState;
}

function useMockAuth(): AppAuthState {
  return mockAuthState;
}

/**
 * Единая точка входа для состояния авторизации во всём приложении.
 *
 * Если VITE_AUTH_DISABLED=true (см. .env) — возвращает "заглушку",
 * которая всегда считается авторизованной, без обращения к Keycloak.
 * Это нужно только для локальной проверки функционала ДО настройки
 * Keycloak. Выбор между реальным хуком и заглушкой происходит один
 * раз при загрузке модуля (значение VITE_AUTH_DISABLED фиксировано на
 * весь жизненный цикл сборки), поэтому правила хуков React не
 * нарушаются — компонент всегда вызывает один и тот же хук.
 */
export const useAppAuth: () => AppAuthState = AUTH_DISABLED ? useMockAuth : useRealAuth;
