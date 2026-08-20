# Frontend Product Requirements Document — Modular Commerce Platform

*Companion to the backend PRD. Every endpoint, JSON shape, validation rule, and permission string below was read directly out of the current backend source (branch `payments-external-provider-integration`, and the shared modules on `main`) — not inferred. Where the request template this document was built from assumed something the backend doesn't actually have, that divergence is called out explicitly rather than silently invented, so the frontend team isn't handed a contract that doesn't exist.*

---

## 0. Reality check — read this before the rest

The brief this PRD was built from assumed: a local `POST /api/users/login` + `POST /api/users/refresh` pair, a `GetMe` call, a `UserPolicyConstants.cs` file, HATEOAS links in responses, and paginated list endpoints. **None of these exist in this backend.** Here is what actually exists instead, and how it substitutes:

| Assumed | Reality | Substitute used in this PRD |
|---|---|---|
| `POST /api/users/login` on this API | Auth is delegated entirely to **Keycloak** (realm `eshop-realm`, public client `eshop-public`) | OIDC Authorization Code + PKCE redirect to Keycloak's own login page — §1 |
| `POST /api/users/refresh` | No refresh endpoint on this API | Keycloak's own token refresh, via `keycloak-js` `updateToken()` — §1.4 |
| `GetMe` endpoint | No such endpoint | Decode the access token client-side — it already carries identity + roles — §1.3 |
| `UserPolicyConstants.cs` | Four separate files, one per module: `Authorization/Permissions.cs` in Catalog, Customers, Orders, Warehouse | Full list in §3.1 |
| HATEOAS `_links` in responses | No response in this API includes links | Button visibility is computed purely client-side from JWT claims — §3 |
| Paginated list endpoints (`page`, `pageSize`, `totalCount`, `totalPages`) | Exactly one list endpoint exists (`GET /api/customers`) and it returns a bare, unpaginated JSON array | Client-side paging over the full array, with a flagged backend gap — §4 |

**A bigger structural gap that affects page design directly: three of the four modules have no read/query endpoints at all.** Catalog has no `GET` for products, Orders has no `GET` for orders, Warehouse has no `GET` for stock. This means an "Edit Product," "Order Detail," or "Stock Level" page **cannot be pre-populated from the server** as things stand — each affected page below says exactly what that means for its UI and flags it as a backend prerequisite, not something the frontend can work around.

---

## 1. Authentication Flow

### 1.1 Identity provider

- **Realm**: `eshop-realm`
- **Authority (dev)**: `http://localhost:8080`
- **Client**: `eshop-public` — `publicClient: true`, `standardFlowEnabled: true` (Authorization Code), `directAccessGrantsEnabled: true`. No client secret (it's a public SPA client).
- **Registered redirect URI (dev)**: `https://localhost:7089/*` — this is the **API's** dev port, not an Angular dev-server port. **Before the Angular app can log in, add its actual origin (e.g. `http://localhost:4200/*`) to this client's Redirect URIs and Web Origins in the Keycloak realm config** — this is a required setup step, not optional.
- The API validates tokens via `AddKeycloakJwtBearer("keycloak", realm: "eshop-realm", audience: "account")` — note the configured audience is `account`, not `eshop-public`.

### 1.2 Login

There is no username/password form posted to this backend. Use **Authorization Code + PKCE**, redirecting to Keycloak's own hosted login page. In Angular, use `keycloak-angular` (wraps `keycloak-js`) rather than a generic OIDC library, since Keycloak is already fixed as the IdP:

```ts
// app.config.ts
provideKeycloakAngular({
  config: {
    url: 'http://localhost:8080',
    realm: 'eshop-realm',
    clientId: 'eshop-public',
  },
  initOptions: {
    onLoad: 'check-sso', // or 'login-required' to force login app-wide
    pkceMethod: 'S256',
    silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
    checkLoginIframe: false, // see note below
  },
})
```

⚠️ **`checkLoginIframe: false` is required, not optional, in a modern browser.** `keycloak-js`'s ongoing session monitor reads Keycloak's session cookie from inside a hidden iframe on Keycloak's own origin — a third-party-cookie access pattern. Chrome (and Safari/Firefox before it) increasingly blocks that by default, and the observed symptom isn't a clean failure — it's `init()` hanging until `Error: Timeout when waiting for 3rd party check iframe message`, which blocks the entire app from rendering (see §0). Token freshness doesn't depend on this check anyway — `includeBearerTokenInterceptor` already refreshes proactively before each API call — so disabling it costs nothing.

Flow: user hits the app → `keycloak-js` redirects to `http://localhost:8080/realms/eshop-realm/protocol/openid-connect/auth?...` → user authenticates on Keycloak's page → Keycloak redirects back with an authorization code → `keycloak-js` exchanges it for tokens against `http://localhost:8080/realms/eshop-realm/protocol/openid-connect/token`. The app never sees the user's password.

**Token response** (from Keycloak, not this API) — shape for reference:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "id_token": "eyJhbGciOiJSUzI1NiIs...",
  "session_state": "3f7a...",
  "scope": "openid profile email"
}
```

### 1.3 Storing the token & the "GetMe" equivalent

Let `keycloak-js` hold the tokens (in-memory, managed by the library) rather than writing them to `localStorage` — avoids exposing a long-lived bearer token to XSS. An HTTP interceptor attaches it to every call to the API:

```ts
Authorization: Bearer <access_token>
```

There is no `GetMe` endpoint. The access token **is** the user profile: decode it (`keycloak.tokenParsed`) to get identity and roles. Example decoded payload (fields that matter to the frontend):

```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "preferred_username": "jane.doe",
  "given_name": "Jane",
  "family_name": "Doe",
  "email": "jane.doe@example.com",
  "resource_access": {
    "eshop-public": {
      "roles": ["catalog:view", "catalog:create", "order:update", "customer:view"]
    }
  },
  "exp": 1755610000,
  "iat": 1755609700
}
```

`resource_access.eshop-public.roles` is exactly the list the backend itself reads (`KeycloakRolesClaimsTransformation`, `src/Common/Modular.Authorization/KeycloakRolesClaimsTransformation.cs`) to build its own permission claims — the frontend should read the **same claim**, so client-side and server-side authorization are checking the same source of truth. See §3.

### 1.4 Refresh-on-401

```ts
// auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const keycloak = inject(Keycloak);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        return from(keycloak.updateToken(30)).pipe(
          switchMap((refreshed) => {
            if (!refreshed && !keycloak.token) {
              keycloak.login(); // refresh token itself expired — full re-login
              return EMPTY;
            }
            const retried = req.clone({
              setHeaders: { Authorization: `Bearer ${keycloak.token}` },
            });
            return next(retried);
          }),
          catchError(() => { keycloak.login(); return EMPTY; }),
        );
      }
      return throwError(() => err);
    }),
  );
};
```

`keycloak.updateToken()` transparently calls Keycloak's refresh-token grant on the same token endpoint used for login — there is no separate refresh endpoint to know about.

### 1.5 Logout

```ts
keycloak.logout({ redirectUri: window.location.origin + '/login' });
```

This hits Keycloak's `end-session` endpoint, clears the Keycloak session, and redirects — clear any app-local state (e.g. a permissions cache) in the same call.

---

## 2. Pages

Every page below lists only endpoints that actually exist. Response/request JSON is copied field-for-field from the C# request/response records — **note that ASP.NET Core's default `System.Text.Json` serializes PascalCase C# properties as camelCase JSON**, which is reflected in every example.

### 2.1 Catalog

#### Page: Create Product

| | |
|---|---|
| Endpoint | `POST /api/products` |
| Permission | `catalog:create` |

Request:
```json
{
  "sku": "WIDGET-001",
  "name": "Chrome Widget",
  "description": "A standard chrome-plated widget.",
  "price": 19.99
}
```

Success response: **201 Created**. ⚠️ The Location header is built as `/api/products/{sku}`, but the handler actually returns `Unit` (a void marker), not the SKU — **the header and body are not usable**; the response body should be ignored and the SKU you already submitted should be used as the identifier going forward. (This mismatch exists in code today; flag it to the backend team, don't work around it client-side.)

UI: form with 4 fields (SKU, Name, Description, Price). No product list exists to check for duplicates before submit — the only feedback on a duplicate SKU is the 409 error below.

Validation (client-side, since the backend enforces almost none of this itself — see the callout after the table):

| Field | Rule |
|---|---|
| Sku | required |
| Name | required |
| Description | required |
| Price | required, must be a number ≥ 0 |

⚠️ **The backend's own validation here is thin and worth compensating for in the form:** Create has *no* FluentValidation validator at all — only in-constructor guard clauses. A negative price throws an unhandled exception server-side (surfaces as a raw 500, not a clean 400) rather than a validation error, so **enforce `price ≥ 0` client-side**; the API will not give you a friendly message if you don't. Sku is capped at 15 characters and Name at 50 in the database, but neither limit is validated by the API — enforce `maxlength="15"` / `maxlength="50"` on the inputs, because exceeding them server-side risks a raw database error rather than a 400.

Errors:
- ⚠️ `500`, **not** `409` — SKU already exists: `{ "type": "...", "title": null, "status": 500, "detail": "Product with SKU 'WIDGET-001' already exists." }`. Verified directly in `ProductErrors.cs`: this case is built with `Error.Failure(...)`, not `Error.Conflict(...)`, so `ErrorOrExtensions.ToResult` maps it to a bare `500` despite the message reading like a conflict. Match on the `detail` text if you want a distinct "SKU taken" UI state — there is no status-code signal to branch on here.
- `500` — negative price, or any unhandled failure.

#### Page: Edit Product

| | |
|---|---|
| Endpoint | `PUT /api/products` |
| Permission | `catalog:update` |

⚠️ **Blocking gap: there is no `GET /api/products` or `GET /api/products/{sku}`.** This page cannot pre-fill current values — the operator must already know (or re-type) the product's current Name/Description/Price, or this is effectively a blind overwrite. Recommend treating this as a P0 backend prerequisite (add a product read endpoint) before shipping this page for real use; until then, build it as a bare "overwrite by SKU" form and say so in the UI copy.

Request:
```json
{
  "sku": "WIDGET-001",
  "name": "Chrome Widget (Updated)",
  "description": "A standard chrome-plated widget, now with a mirror finish.",
  "price": 24.99
}
```

Validation (backend-enforced this time — Update *does* have a validator):

| Field | Rule |
|---|---|
| Sku | required |
| Name | required |
| Description | required |
| Price | required, **> 0** (strictly greater than zero — a `0.00` price is rejected here, unlike Create) |

Success: **200 OK**, empty body (despite the endpoint's own Swagger metadata claiming it can return 201 — it never does; that's a doc bug in the API, not something the frontend should code against).

Errors: `400` (validation — see §Appendix for the exact shape and the "only one error at a time" caveat), `404` (SKU not found).

⚠️ Updating a product raises no event on the backend — other modules' cached copies of this product's name/description/price will not reflect the edit. Not a frontend concern to solve, but worth knowing if data looks stale elsewhere in the system after using this page.

---

### 2.2 Customers

#### Page: Customer List

| | |
|---|---|
| Endpoint | `GET /api/customers` |
| Permission | `customer:view` |

Response — a **bare array**, not paginated (see §4):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "firstName": "Jane",
    "middleName": null,
    "lastName": "Doe",
    "street": "742 Evergreen Terrace",
    "city": "Springfield",
    "zip": "62704",
    "state": "IL",
    "email": "jane.doe@example.com",
    "phone": null
  }
]
```

⚠️ Note this shape only includes the **billing** address (Street/City/Zip/State) — shipping address is never returned by any Customers endpoint.

Table columns: Name (First + Middle + Last), Street, City, State, Zip, Email, Phone. Row action → Customer Detail (needs the `id`).

#### Page: Customer Detail

| | |
|---|---|
| Endpoint | `GET /api/customers/id?id={guid}` |
| Permission | **None — any authenticated user** (⚠️ this is the one read endpoint with no permission check at all, inconsistent with the list endpoint requiring `customer:view`) |

⚠️ **The route is literally `/api/customers/id`, not `/api/customers/{id}`.** There's no `{id}` route token in the backend code — the `id` is bound from the **query string**, not the path. Call it exactly as: `GET /api/customers/id?id=3fa85f64-5717-4562-b3fc-2c963f66afa6`. This looks like an unintentional backend bug (worth a fix request), but it's what's live today.

Response: same shape as the list-endpoint row (no shipping address here either — no endpoint returns it).

Errors: `404` if the ID doesn't match a customer.

#### Page: Register Customer

| | |
|---|---|
| Endpoint | `POST /api/customers` |
| Permission | `customer:create` |

Request:
```json
{
  "firstName": "Jane",
  "middleName": null,
  "lastName": "Doe",
  "address": {
    "street": "742 Evergreen Terrace",
    "city": "Springfield",
    "zip": "62704",
    "state": "IL"
  },
  "shippingAddress": null,
  "email": "jane.doe@example.com",
  "phone": null,
  "primaryContactType": 0
}
```

⚠️ `primaryContactType` is a plain C# `enum { Email = 0, Phone = 1 }` with no string-enum converter configured on the API — **it serializes and deserializes as an integer, not `"Email"`/`"Phone"`**. In the form, bind a radio/select to the label but submit `0` or `1`.

If the customer's shipping address is the same as billing, submit `shippingAddress: null` — the backend defaults it to the billing address itself; don't duplicate the values client-side.

Validation:

| Field | Rule |
|---|---|
| firstName | required, max 50 chars |
| lastName | required, max 50 chars |
| middleName | optional, max 50 chars |
| address.street | required, max 100 chars |
| address.city | required, max 50 chars |
| address.state | required, max 50 chars |
| address.zip | required, max 10 chars |
| shippingAddress.* | *(only checked if `shippingAddress` is non-null)* — street/city/state max lengths as above (not required individually) |
| email | must be a valid email format, max 80 chars — only checked if provided |
| phone | max 50 chars — only checked if provided |
| *(cross-field, enforced in the handler, not the validator)* | at least one of `email`/`phone` must be present, and whichever one `primaryContactType` points at must itself be non-null — enforce both client-side too, since violating either surfaces as a single generic 400 |

Success: **201 Created**, body is a **bare GUID string** (the new customer's ID) — e.g. `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` — not wrapped in an object. Location header: `/api/customers/{that guid}` (informational only — remember the actual GET route needs `?id=`, not a path segment, so don't follow this header literally).

Errors: `400` (validation, or missing-contact-info rule), `500` (contact uniqueness conflict or unexpected failure — email/phone uniqueness violations surface as a generic 500 here, not a clean 409, because the handler wraps creation in a try/catch that returns `Error.Failure` for anything unexpected).

#### Page: Edit Customer

| | |
|---|---|
| Endpoint | `PUT /api/customers/id?id={guid}` |
| Permission | `customer:update` |

Same request shape as Register (all fields required again, not a partial patch — you must resend the full object even for a one-field change), same validation table. ⚠️ Same literal-`id`-in-query-string caveat as the Detail page.

⚠️ Because Customer Detail's response never includes the shipping address, **the Edit form cannot pre-fill the "shipping address differs from billing" section from server data** — the operator must know it's different and re-enter it, or the edit will silently reset shipping-to-billing (since submitting `shippingAddress: null` defaults it to billing). Flag this clearly in the UI (e.g. a checkbox "shipping address differs from billing," unchecked by default, rather than showing stale/blank data as if it were current).

Success: **200 OK**, empty body.

Errors: `400`, `404` (`{"detail": "Customer does not exist."}`).

---

### 2.3 Orders

⚠️ **Blocking gap, bigger than Catalog's: there is no `GET` endpoint anywhere in Orders.** No order list, no single-order lookup, no way to see an order's current status, items, or total after creation. A conventional "Orders" page (browse, open, see status) **cannot be built against this API today.** The seven endpoints below are write-only actions; document them as an **Order Workspace** flow, not a CRUD page, and treat adding read endpoints as a P0 backend prerequisite before this module has a real UI.

Interim UI approach: after Create Order returns an order ID, keep it in the browser's session/local state (e.g. "your open orders this session") so the subsequent action buttons (Add Product, Submit, Cancel, etc.) have an ID to call against — there's no way to rediscover that ID later from the server.

#### Action: Create Order

| | |
|---|---|
| Endpoint | `POST /api/orders/create` |
| Permission | `order:create` |

Request:
```json
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    { "productId": 42, "quantity": 3, "price": { "value": 19.99 } }
  ]
}
```

⚠️ **`price` is a nested object `{ "value": <decimal> }`, not a bare number.** The backend's `Price` type is a C# record with a single `Value` property, and it serializes as an object. This applies to every endpoint below that takes a price. Sending `"price": 19.99` will fail to bind.

⚠️ The `items` array binds directly to the backend's internal `OrderItem` model (not a lean request DTO) — only send `productId`, `quantity`, `price`; do not send `orderId`, `product`, or `shippedStatus` fields even though they technically exist on that type — they're not meant to come from the client and sending them has no defined effect.

Success: **201 Created**, body is the new order's **bare GUID string**.

Errors: `400` — order already exists (only possible if you generate and reuse your own order IDs, which this page shouldn't do — the ID is server-generated).

#### Action: Add Product to Order

| | |
|---|---|
| Endpoint | `POST /api/orders/add/{orderId}` |
| Permission | `order:update` |

Request: `{ "productId": 42, "quantity": 2, "price": { "value": 19.99 } }`

Success: **201 Created**. ⚠️ Same `Unit`-vs-real-value bug as Catalog's Create — ignore the response body and Location header entirely; they don't contain what their shape implies.

Errors: `400` — order not found, product not found, or insufficient stock (`{"detail": "Product 42 does not have enough stock."}` style message — exact wording comes from `OrderErrors`, treat it as a generic user-facing string, not something to pattern-match on).

#### Action: Increase / Decrease Item Quantity

| | |
|---|---|
| Endpoints | `POST /api/orders/increase-quantity/{orderId}`, `POST /api/orders/decrease-quantity/{orderId}` |
| Permission | `order:update` |

Request (both): `{ "productId": 42, "quantity": 1 }` — the *delta* to add or remove, not the new total.

Success: **204 No Content**.

Errors: `400` — product not on the order, or (decrease only) requested amount exceeds what's currently on the order.

#### Action: Remove Product from Order

| | |
|---|---|
| Endpoint | `POST /api/orders/remove/{orderId}` |
| Permission | `order:update` |

Request: `{ "productId": 42 }`. Success: **200 OK**, empty body.

"Product not on the order" returns a clean `400`: `{ "status": 400, "title": "Bad Request", "detail": "Product with ID '42' is not placed in order '<orderId>' and cannot be removed." }` (`Order.RemoveItem` previously threw an unhandled exception here, surfacing as a bare, detail-less `500`; this was fixed to return `ErrorOr` like every other Orders action). Still enable the "Remove" button only for line items the workspace already knows are on the order, since there's no order-read endpoint to double-check against — this is about correct error handling on a stale click, not a substitute for that check.

#### Action: Submit Order

| | |
|---|---|
| Endpoint | `POST /api/orders/submit/{orderId}` |
| Permission | ⚠️ `order:create` — **not** `order:update`, unlike every other mutating Orders action. This looks like a backend inconsistency, not an intentional design choice, but code the permission check to match it as it is today, and flag the inconsistency to the backend team. |

No request body. Success: **204 No Content**. This kicks off the payment/shipping saga server-side — there is no way for the frontend to poll or be told when that finishes, since there's no order-read endpoint (see the Notifications note below).

Errors: `400` — illegal transition (e.g. order already Shipped/Canceled).

#### Action: Cancel Order

| | |
|---|---|
| Endpoint | `POST /api/orders/cancel/{orderId}` |
| Permission | `order:update` |

No request body. Success: **204 No Content**. Errors: `400` — illegal transition (only Pending/Submitted orders can be canceled).

**Order status values** (for reference — never returned by any response today, but useful once a read endpoint exists): `Pending` (1) → `Submitted` (2) → `Shipped` (3), or → `Canceled` (4) from Pending/Submitted only. Shipped and Canceled are terminal.

---

### 2.4 Warehouse

⚠️ Same read-gap as Orders: **no `GET` endpoint exists for stock levels.** These four actions are blind adjustments — the operator types a SKU and a quantity with no on-screen confirmation of current stock. Flag a stock-read endpoint as a P0 backend prerequisite for a real inventory page; until then, this is a "stock adjustment form," not an "inventory" page.

All four require permission `warehouse:update` (the module also defines `warehouse:view/create/delete`, but nothing in the backend currently checks them — don't build UI gated on those three; they're inert).

#### Action: Receive Stock

| | |
|---|---|
| Endpoint | `POST /api/warehouse/received/{sku}` |

Request: `{ "quantity": 100 }`. Validation: `quantity > 0`. Success: **204 No Content**. Errors: `400` — quantity ≤ 0, or SKU doesn't exist yet (a product stream is only created when Catalog publishes a `ProductCreatedIntegrationEvent`, so a brand-new SKU that hasn't been through Create Product yet will fail here).

#### Action: Ship Stock

| | |
|---|---|
| Endpoint | `POST /api/warehouse/shipping/{sku}` |

Request: `{ "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 5 }`. Validation: `quantity > 0`, `orderId` required (non-empty GUID).

Success: **204 No Content**. Errors: `400` — insufficient stock, or a `409`-style concurrency conflict surfaced as a generic error if two shipments race on the same SKU (`{"detail": "Product with SKU 'WIDGET-001' was modified concurrently, please retry."}`) — the UI should offer a retry action on this specific message.

#### Action: Manually Increase / Decrease Stock

| | |
|---|---|
| Endpoints | `POST /api/warehouse/increased/{sku}`, `POST /api/warehouse/decreased/{sku}` |

Request (both): `{ "quantity": 10, "reason": "Cycle count correction" }`. Validation: `quantity > 0`, `reason` required (non-empty). Success: **204 No Content**. Errors: `400` — decrease exceeding current stock, or same concurrency-conflict message as Ship Stock.

---

## 3. Conditional Rendering

### 3.1 Permission strings

Read from the four `Authorization/Permissions.cs` files (there is no single shared constants file):

| Module | Permission | Used by |
|---|---|---|
| Catalog | `catalog:view` | *(defined, not enforced by any endpoint today)* |
| Catalog | `catalog:create` | Create Product |
| Catalog | `catalog:update` | Edit Product |
| Catalog | `catalog:delete` | *(defined, no endpoint exists)* |
| Customers | `customer:view` | Customer List |
| Customers | `customer:create` | Register Customer |
| Customers | `customer:update` | Edit Customer |
| Customers | `customer:delete` | *(defined, no endpoint exists)* |
| Orders | `order:view` | *(defined, no endpoint exists)* |
| Orders | `order:create` | Create Order, **and Submit Order** (see the flag in §2.3) |
| Orders | `order:update` | Add/Increase/Decrease/Remove Product, Cancel Order |
| Orders | `order:delete` | *(defined, no endpoint exists)* |
| Warehouse | `warehouse:view` | *(defined, not enforced by any endpoint today)* |
| Warehouse | `warehouse:create` | *(defined, not enforced by any endpoint today)* |
| Warehouse | `warehouse:update` | all four stock actions |
| Warehouse | `warehouse:delete` | *(defined, no endpoint exists)* |
| — | *(none)* | Customer Detail — any authenticated user |

Don't build UI toggles for the "defined, not enforced" rows — showing/hiding a button by a permission the backend never actually checks would be pure decoration with no security meaning, and worse, might hide an action a user's role would otherwise be allowed to hit directly via the API.

### 3.2 Mechanism

There's no dedicated "my permissions" or HATEOAS-links endpoint to call. Read `resource_access.eshop-public.roles` directly off the decoded access token — it's the exact same claim the backend itself reads server-side, so client and server checks stay aligned by construction:

```ts
export class PermissionService {
  private keycloak = inject(Keycloak);

  has(permission: string): boolean {
    const roles = this.keycloak.tokenParsed?.resource_access?.['eshop-public']?.roles ?? [];
    return roles.includes(permission);
  }
}
```

```html
<button *ngIf="permissions.has('catalog:create')" (click)="createProduct()">New Product</button>
```

Because there's no HATEOAS, this check is advisory only — **every page must still handle a `403` gracefully** (the button being visible doesn't guarantee the call succeeds; role assignments can be out of sync, or — as with `order:create` gating Submit — the permission model itself has inconsistencies documented above).

### 3.3 Navigation

Hide a top-level nav entry if the user holds none of the permissions anything behind it needs:

| Nav item | Visible if user has any of |
|---|---|
| Catalog | `catalog:create`, `catalog:update` |
| Customers | `customer:view`, `customer:create`, `customer:update` *(Customer Detail is always reachable by any authenticated user, so don't gate the nav entry on a permission that would hide it entirely for someone who could otherwise open a customer by direct link)* |
| Orders | `order:create`, `order:update` |
| Warehouse | `warehouse:update` |

---

## 4. Paging

**Reality: nothing in this backend is paginated.** The only list endpoint, `GET /api/customers`, returns every row as a bare JSON array (§2.2) — no `page`, `pageSize`, `totalCount`, or `totalPages` exist anywhere in the API today.

### Interim approach (client-side)

Fetch the full array once, page it in the browser:

```ts
readonly pageSize = signal(25);
readonly pageIndex = signal(0);
readonly pagedCustomers = computed(() => {
  const start = this.pageIndex() * this.pageSize();
  return this.allCustomers().slice(start, start + this.pageSize());
});
```

Use a standard paginator component (page-size selector: 10/25/50/100) driven entirely off the in-memory array length. This is fine at small scale but degrades as the customer table grows — every page load pulls the entire table over the wire.

### Recommended backend addition (not yet implemented — flag as a follow-up request, not something to build around silently)

```
GET /api/customers?page=1&pageSize=25
```
```json
{
  "items": [ { "id": "...", "firstName": "Jane", "...": "..." } ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 143,
  "totalPages": 6
}
```
Until this exists, build the paginator component against the shape above behind a feature flag or a thin adapter, so swapping from client-side to server-side paging later doesn't require rewriting the table component — but ship the client-side version first, since that's what the real API supports today.

---

## Appendix A: Error response shape

Every error from every endpoint in this API follows the same RFC 7807 Problem Details shape (`Modular.Common.ErrorOrExtensions.ToResult`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Customer does not exist."
}
```

| Status | Meaning | UI treatment |
|---|---|---|
| 400 | Validation failure or a rejected business rule (e.g. illegal order-status transition, insufficient stock) | Show `detail` as an inline or toast message |
| 401 | Missing/expired token | Trigger the refresh-then-retry flow (§1.4); if that also fails, redirect to login |
| 403 | Authenticated, but missing the required permission | Show "you don't have permission to do this" and don't retry |
| 404 | Entity not found | Show `detail`; typically means a stale ID (e.g. a customer deleted by someone else) |
| 409 | Conflict — the only real examples today are Warehouse's `Product.NotEnoughQuantity` and `Product.ConcurrentModification` (an event-stream optimistic-concurrency clash). ⚠️ Catalog's "duplicate SKU" case *reads* like a conflict but is actually a `500` — see §2.1 | Show `detail`; for the concurrency case specifically, offer a "retry" action rather than treating it as a dead end |
| 500 | Unexpected/unhandled failure | Generic "something went wrong, try again" — `detail` may leak an exception message in dev but shouldn't be trusted as user-facing copy in prod |

⚠️ **Only ever one error comes back per request.** The validator can find multiple failing fields, but `ToResult()` only surfaces `response.FirstError` — the `detail` string describes exactly one problem, and the JSON body has no field-level breakdown (no `errors: { fieldName: [...] }` dictionary — that's a common ASP.NET convention this API does *not* use). Practically: show `detail` as a single global form message, not something you can map back onto individual inputs; the user may need to fix one thing, resubmit, and see the next error in sequence. Client-side validation (this document's per-page tables) exists specifically to catch as much as possible before that round-trip, since the server won't tell you everything wrong in one shot.

## Appendix B: Endpoint quick reference

| Method | Route | Permission | Success | Page |
|---|---|---|---|---|
| POST | `/api/products` | `catalog:create` | 201 (body unusable) | Create Product |
| PUT | `/api/products` | `catalog:update` | 200, empty | Edit Product |
| GET | `/api/customers` | `customer:view` | 200, array | Customer List |
| GET | `/api/customers/id?id=` | *(authenticated)* | 200 | Customer Detail |
| POST | `/api/customers` | `customer:create` | 201, GUID string | Register Customer |
| PUT | `/api/customers/id?id=` | `customer:update` | 200, empty | Edit Customer |
| POST | `/api/orders/create` | `order:create` | 201, GUID string | Create Order |
| POST | `/api/orders/add/{orderId}` | `order:update` | 201 (body unusable) | Add Product |
| POST | `/api/orders/increase-quantity/{orderId}` | `order:update` | 204 | Increase Quantity |
| POST | `/api/orders/decrease-quantity/{orderId}` | `order:update` | 204 | Decrease Quantity |
| POST | `/api/orders/remove/{orderId}` | `order:update` | 200, empty | Remove Product |
| POST | `/api/orders/submit/{orderId}` | ⚠️ `order:create` | 204 | Submit Order |
| POST | `/api/orders/cancel/{orderId}` | `order:update` | 204 | Cancel Order |
| POST | `/api/warehouse/received/{sku}` | `warehouse:update` | 204 | Receive Stock |
| POST | `/api/warehouse/shipping/{sku}` | `warehouse:update` | 204 | Ship Stock |
| POST | `/api/warehouse/increased/{sku}` | `warehouse:update` | 204 | Increase Stock |
| POST | `/api/warehouse/decreased/{sku}` | `warehouse:update` | 204 | Decrease Stock |

## Appendix C: Angular environment config

```ts
export const environment = {
  apiBaseUrl: 'https://localhost:7089',       // Modular.WebApi — confirm against your launch profile
  keycloak: {
    url: 'http://localhost:8080',
    realm: 'eshop-realm',
    clientId: 'eshop-public',
  },
};
```

Before login will work end-to-end, add the Angular dev-server origin (`http://localhost:4200` by default) to the `eshop-public` client's **Valid Redirect URIs** and **Web Origins** in the Keycloak realm — as exported today (`keycloak-config/eshop-realm-export.json`) it only allows `https://localhost:7089/*`, the API's own port. This is a one-time Keycloak admin console change, not something the Angular app can configure around.
