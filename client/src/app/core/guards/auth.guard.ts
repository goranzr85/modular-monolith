import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { createAuthGuard, AuthGuardData } from 'keycloak-angular';
import { environment } from '../../../environments/environment';

/**
 * Route data contract:
 *   { permission: 'catalog:create' }              — single permission required
 *   { permission: ['order:create', 'order:update'] } — any one of these required
 * Omit `permission` entirely for "just needs to be signed in".
 */
const isAccessAllowed = async (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
  authData: AuthGuardData,
) => {
  const { authenticated, grantedRoles, keycloak } = authData;

  if (!authenticated) {
    await keycloak.login({ redirectUri: window.location.origin + state.url });
    return false;
  }

  const required = route.data?.['permission'] as string | string[] | undefined;
  if (!required) {
    return true;
  }

  const requiredList = Array.isArray(required) ? required : [required];
  const granted = grantedRoles.resourceRoles[environment.keycloak.clientId] ?? [];
  const allowed = requiredList.some((p) => granted.includes(p));

  if (!allowed) {
    const router = inject(Router);
    return router.parseUrl('/forbidden');
  }

  return true;
};

export const authGuard: CanActivateFn = createAuthGuard(isAccessAllowed);
