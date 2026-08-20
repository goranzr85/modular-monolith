export interface OrderLineItem {
  productId: number;
  quantity: number;
  price: number;
}

/** POST /api/orders/create request shape. `price` is a nested
 *  `{ value: number }` object on every Orders endpoint — unlike Catalog,
 *  where price is a bare number (docs/frontend-prd.md §2.3, Create Order). */
export interface CreateOrderRequest {
  customerId: string;
  items: { productId: number; quantity: number; price: { value: number } }[];
}

export interface AddProductRequest {
  productId: number;
  quantity: number;
  price: { value: number };
}

export interface ProductQuantityRequest {
  productId: number;
  quantity: number;
}

export interface RemoveProductRequest {
  productId: number;
}

export type OrderWorkspaceStatus = 'Pending' | 'Submitted' | 'Canceled';

/**
 * Purely client-side, in-memory record of what this session did to an
 * order — there is no GET /api/orders anywhere, so this is the only place
 * an order's current items/status can be read back from (docs/frontend-prd.md §2.3).
 * Lost on page reload; that's a stated limitation, not a bug to work around.
 */
export interface OrderWorkspaceState {
  orderId: string;
  customerId: string;
  customerLabel: string;
  items: OrderLineItem[];
  status: OrderWorkspaceStatus;
}
