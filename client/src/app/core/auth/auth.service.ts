import { Injectable, computed, inject, signal } from '@angular/core';
import Keycloak from 'keycloak-js';
import { KEYCLOAK_EVENT_SIGNAL, KeycloakEventType } from 'keycloak-angular';
import { environment } from '../../../environments/environment';

/**
 * Claims this app actually reads off the decoded access token — the same
 * resource_access.<clientId>.roles array the backend's
 * KeycloakRolesClaimsTransformation reads server-side to build permission
 * claims. There is no GetMe endpoint; the token *is* the profile.
 */
interface DecodedAccessToken {
  sub?: string;
  preferred_username?: string;
  given_name?: string;
  family_name?: string;
  email?: string;
  resource_access?: Record<string, { roles: string[] }>;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly keycloak = inject(Keycloak);
  private readonly keycloakEvent = inject(KEYCLOAK_EVENT_SIGNAL);

  /** Recomputed whenever keycloak-angular reports a new lifecycle event
   *  (Ready, AuthSuccess, AuthRefreshSuccess, ...) — that's our cue the
   *  underlying tokenParsed may have changed. */
  private readonly tick = computed(() => this.keycloakEvent());

  readonly isAuthenticated = computed<boolean>(() => {
    this.tick();
    return !!this.keycloak.authenticated;
  });

  private readonly claims = computed<DecodedAccessToken>(() => {
    this.tick();
    return (this.keycloak.tokenParsed as DecodedAccessToken) ?? {};
  });

  readonly displayName = computed<string>(() => {
    const c = this.claims();
    const name = [c.given_name, c.family_name].filter(Boolean).join(' ');
    return name || c.preferred_username || 'Signed in user';
  });

  readonly permissions = computed<ReadonlySet<string>>(() => {
    const roles = this.claims().resource_access?.[environment.keycloak.clientId]?.roles ?? [];
    return new Set(roles);
  });

  /** Toast/error banners need something better than "permission denied" —
   *  keeping the last known state avoids flashing "logged out" during a
   *  silent token refresh. */
  readonly ready = signal(false);

  constructor() {
    if (this.keycloakEvent().type !== KeycloakEventType.KeycloakAngularNotInitialized) {
      this.ready.set(true);
    }
  }

  hasPermission(permission: string): boolean {
    return this.permissions().has(permission);
  }

  hasAnyPermission(permissions: readonly string[]): boolean {
    return permissions.some((p) => this.permissions().has(p));
  }

  async login(redirectUri?: string): Promise<void> {
    await this.keycloak.login({ redirectUri: redirectUri ?? window.location.href });
  }

  async logout(): Promise<void> {
    await this.keycloak.logout({ redirectUri: window.location.origin + '/login' });
  }
}
