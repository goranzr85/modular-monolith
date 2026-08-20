# Operator Console

Angular frontend for the Modular Commerce Platform backend, built against
[`docs/frontend-prd.md`](../docs/frontend-prd.md) — read that first for the exact API
contracts this app calls, and for every known backend gap/quirk (no read
endpoints for Products/Orders/Warehouse stock, the `order:create`-gated
Submit action, the query-bound `/api/customers/id` route, etc.). This
README covers the frontend itself.

## Stack

- Angular 22, standalone components only, zoneless change detection
- Signals for all local/component state
- Tailwind CSS v4 (CSS-first config in `src/styles.css`), light/dark via a
  `ThemeService` that toggles a class on `<html>` and persists to `localStorage`
- `keycloak-angular` / `keycloak-js` for auth — this backend has no login
  endpoint of its own; the API's `resource_access.eshop-public.roles` claim
  *is* the permission model, and this app reads the same claim client-side

## One-time setup: Keycloak

The realm export at `../src/modular-monolith.AppHost/keycloak-config/eshop-realm-export.json`
only allows the API's own dev origin. Before this app can log in:

1. In the Keycloak admin console, open the `eshop-public` client → **Settings**.
2. Add `http://localhost:4200/*` to **Valid Redirect URIs** and `http://localhost:4200` to **Web Origins**.
3. Under **Roles**, create one client role per permission string this app checks
   (see `src/app/core/auth/permissions.ts`) — e.g. `catalog:create`,
   `order:update`, `warehouse:update`, etc. — and assign them to test users.
   These aren't in the seed realm export; RBAC won't do anything meaningful
   until they exist.

## Running

```bash
npm install
npm start          # ng serve, http://localhost:4200
```

Requires the backend (`Modular.WebApi`) and Keycloak running — easiest via
the AppHost: `dotnet run --project ../../modular-monolith/src/modular-monolith.AppHost`
(adjust the path to wherever you have the backend repo checked out; this
frontend lives in its own worktree/branch).

`src/environments/environment.development.ts` points at `https://localhost:7089/api`
and `http://localhost:8080` for Keycloak — update if your local ports differ.

```bash
npm run build       # production build, output in dist/client
npm test            # Vitest unit tests
```

## Architecture

```
src/app/
  core/            Singletons: auth (AuthService, permissions), guards,
                    HTTP interceptors, cross-cutting services (theme, toasts),
                    shared models (ProblemDetails)
  shared/          Reusable, dumb UI: skeleton loaders, buttons, form-field +
                    inline validation, banners, toasts, pagination — plus
                    validators and the *appHasPermission directive
  layout/shell/    App chrome: header, permission-filtered nav, theme toggle
  features/        One folder per backend module (catalog, customers, orders,
                    warehouse), each lazy-loaded via its own *.routes.ts, with
                    a data/ subfolder for the HTTP service + request/response
                    models specific to that feature
  pages/           Top-level pages that aren't tied to one feature (home,
                    forbidden, not-found)
```

Every feature route is lazy-loaded (`loadChildren`) and gated by the
functional `authGuard`, which checks both authentication and an optional
`data: { permission }` on the route — see `core/guards/auth.guard.ts`.

## Notable constraints inherited from the backend

- **No read endpoints for Products, Orders, or Warehouse stock.** Catalog's
  Edit page and all Warehouse actions are blind forms; the Orders module
  keeps client-side, in-memory "workspace" state for orders created this
  session (`features/orders/data/orders-workspace.store.ts`) because there's
  no way to look an order back up from the server. This is a deliberate
  reflection of backend reality, not a shortcut — see `docs/frontend-prd.md`
  for the full reasoning.
- **Customer list pagination is client-side.** The one list endpoint
  (`GET /api/customers`) returns an unpaginated array; `PaginationComponent`
  pages over data already fetched in full.
- **Validation errors are single-message.** The backend never returns a
  per-field error map, so every form shows the server's one `detail` string
  in a banner, on top of client-side field-level validation that catches
  what it can before submit.
