import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  provideKeycloak,
  includeBearerTokenInterceptor,
  INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
} from 'keycloak-angular';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),

    provideKeycloak({
      config: {
        url: environment.keycloak.url,
        realm: environment.keycloak.realm,
        clientId: environment.keycloak.clientId,
      },
      initOptions: {
        onLoad: 'check-sso',
        pkceMethod: 'S256',
        silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
        // Keycloak's ongoing session-iframe monitor depends on third-party
        // cookies to read its session cookie from inside an iframe on a
        // different origin — modern Chrome blocks that by default, which is
        // exactly what surfaces as "Timeout when waiting for 3rd party
        // check iframe message." Token freshness is already handled by
        // includeBearerTokenInterceptor's proactive refresh before each API
        // call, so this check isn't needed and shouldn't depend on
        // third-party cookies working.
        checkLoginIframe: false,
      },
    }),

    provideHttpClient(withInterceptors([includeBearerTokenInterceptor, errorInterceptor])),
    {
      // Only attach the bearer token to calls aimed at this app's own API —
      // never to Keycloak's own endpoints or any third-party host.
      provide: INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
      useValue: [{ urlPattern: new RegExp(`^${escapeRegExp(environment.apiBaseUrl)}`) }],
    },
  ],
};

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
