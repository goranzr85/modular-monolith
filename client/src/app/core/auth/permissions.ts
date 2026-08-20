/**
 * Mirrors the backend's per-module Authorization/Permissions.cs files exactly
 * (there is no single UserPolicyConstants.cs on the backend — see docs/frontend-prd.md §3.1).
 * These are also the literal Keycloak client-role names expected under the
 * `eshop-public` client's resource_access claim.
 */
export const PERMISSIONS = {
  catalog: {
    view: 'catalog:view',
    create: 'catalog:create',
    update: 'catalog:update',
    delete: 'catalog:delete',
  },
  customer: {
    view: 'customer:view',
    create: 'customer:create',
    update: 'customer:update',
    delete: 'customer:delete',
  },
  order: {
    view: 'order:view',
    create: 'order:create',
    update: 'order:update',
    delete: 'order:delete',
  },
  warehouse: {
    view: 'warehouse:view',
    create: 'warehouse:create',
    update: 'warehouse:update',
    delete: 'warehouse:delete',
  },
} as const;

export type Permission =
  (typeof PERMISSIONS)[keyof typeof PERMISSIONS][keyof (typeof PERMISSIONS)[keyof typeof PERMISSIONS]];
