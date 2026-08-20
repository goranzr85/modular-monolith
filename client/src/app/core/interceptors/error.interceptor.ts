import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import Keycloak from 'keycloak-js';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { extractErrorMessage } from '../models/problem-details';

/**
 * Global error handling for every API call. JWT attachment + proactive
 * refresh is handled separately by keycloak-angular's
 * includeBearerTokenInterceptor (see app.config.ts) — by the time a request
 * gets here, the token was already fresh when it left, so a 401 means the
 * session is genuinely no longer valid, not "about to expire."
 *
 * Deliberately narrow: only toasts the errors no single form/page is the
 * right place to show (auth/network/unexpected-failure). 400/404/409 are
 * left to propagate so the calling component can show `detail` inline next
 * to the field/action it belongs to (docs/frontend-prd.md Appendix A) —
 * toasting those too would double up with that inline message.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);
  const keycloak = inject(Keycloak);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          notifications.error('Your session has expired. Please sign in again.');
          void keycloak.login({ redirectUri: window.location.href });
        } else if (error.status === 403) {
          notifications.error("You don't have permission to do that.");
        } else if (error.status === 0) {
          notifications.error('Could not reach the server. Check your connection and try again.');
        } else if (error.status >= 500) {
          notifications.error(extractErrorMessage(error));
        }
      }
      return throwError(() => error);
    }),
  );
};
