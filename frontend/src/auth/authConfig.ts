import type { AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';

/**
 * Настройки OpenID Connect клиента для Keycloak.
 * Значения берутся из переменных окружения (см. .env.example), чтобы не
 * зашивать адрес realm'а и client_id в код — это разные значения для
 * локальной разработки и для боевого стенда.
 */
export const oidcConfig: AuthProviderProps = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  scope: 'openid profile email',
  response_type: 'code',
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  onSigninCallback: () => {
    // Убираем code/state из URL после успешного логина через Keycloak
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
