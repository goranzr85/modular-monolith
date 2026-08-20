import { Injectable, computed, signal } from '@angular/core';
import { OrderLineItem, OrderWorkspaceState } from './orders.models';

/**
 * In-memory record of orders this browser session has touched. There is no
 * GET /api/orders, so this — not the server — is the only source for "what's
 * currently on this order." It does not survive a page reload; that's an
 * accepted limitation of the backend today (docs/frontend-prd.md §2.3), not
 * something to paper over with fake persistence.
 */
@Injectable({ providedIn: 'root' })
export class OrdersWorkspaceStore {
  private readonly orders = signal<Map<string, OrderWorkspaceState>>(new Map());

  readonly openOrders = computed(() => Array.from(this.orders().values()));

  get(orderId: string): OrderWorkspaceState | undefined {
    return this.orders().get(orderId);
  }

  create(orderId: string, customerId: string, customerLabel: string, items: OrderLineItem[]): void {
    this.update(orderId, () => ({
      orderId,
      customerId,
      customerLabel,
      items: [...items],
      status: 'Pending',
    }));
  }

  addOrIncreaseItem(orderId: string, item: OrderLineItem): void {
    this.mutate(orderId, (state) => {
      const existing = state.items.find((i) => i.productId === item.productId);
      if (existing) {
        existing.quantity += item.quantity;
      } else {
        state.items.push({ ...item });
      }
    });
  }

  changeQuantity(orderId: string, productId: number, delta: number): void {
    this.mutate(orderId, (state) => {
      const existing = state.items.find((i) => i.productId === productId);
      if (existing) {
        existing.quantity += delta;
      }
    });
  }

  removeItem(orderId: string, productId: number): void {
    this.mutate(orderId, (state) => {
      state.items = state.items.filter((i) => i.productId !== productId);
    });
  }

  setStatus(orderId: string, status: OrderWorkspaceState['status']): void {
    this.mutate(orderId, (state) => {
      state.status = status;
    });
  }

  private mutate(orderId: string, fn: (state: OrderWorkspaceState) => void): void {
    this.update(orderId, (existing) => {
      if (!existing) return existing as unknown as OrderWorkspaceState;
      const clone: OrderWorkspaceState = { ...existing, items: [...existing.items] };
      fn(clone);
      return clone;
    });
  }

  private update(orderId: string, fn: (existing?: OrderWorkspaceState) => OrderWorkspaceState): void {
    this.orders.update((map) => {
      const next = new Map(map);
      const result = fn(next.get(orderId));
      if (result) {
        next.set(orderId, result);
      }
      return next;
    });
  }
}
