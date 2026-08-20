import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { OrdersService } from '../data/orders.service';
import { OrdersWorkspaceStore } from '../data/orders-workspace.store';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { PERMISSIONS } from '../../../core/auth/permissions';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { NotificationService } from '../../../core/services/notification.service';
import { pricePositive, quantityPositive } from '../../../shared/validators/custom-validators';

@Component({
  selector: 'app-order-workspace',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, ButtonComponent, BannerComponent, FormFieldComponent, HasPermissionDirective, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Orders</a>

    @if (order(); as o) {
      <div class="mt-2 flex items-center justify-between">
        <div>
          <h1 class="text-lg font-semibold text-text">{{ o.customerLabel }}</h1>
          <p class="text-xs text-text-faint">{{ o.orderId }}</p>
        </div>
        <span
          class="rounded-full px-3 py-1 text-xs font-medium"
          [class.bg-accent-soft]="o.status === 'Pending'"
          [class.text-accent]="o.status === 'Pending'"
          [class.bg-success-soft]="o.status === 'Submitted'"
          [class.text-success]="o.status === 'Submitted'"
          [class.bg-danger-soft]="o.status === 'Canceled'"
          [class.text-danger]="o.status === 'Canceled'"
        >
          {{ o.status }}
        </span>
      </div>

      @if (errorMessage(); as msg) {
        <div class="mt-4">
          <app-banner kind="error" [message]="msg" [dismissible]="true" (dismissed)="errorMessage.set(null)" />
        </div>
      }

      @if (o.status !== 'Pending') {
        <div class="mt-4">
          <app-banner kind="info" [message]="statusNote(o.status)" />
        </div>
      }

      <div class="app-card mt-4 overflow-x-auto">
        <table class="w-full min-w-max text-left text-sm">
          <thead class="border-b border-border text-text-muted">
            <tr>
              <th class="px-4 py-3 font-medium">Product ID</th>
              <th class="px-4 py-3 font-medium">Quantity</th>
              <th class="px-4 py-3 font-medium">Unit price</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-border">
            @if (o.items.length === 0) {
              <tr>
                <td colspan="4" class="px-4 py-6 text-center text-text-muted">No items on this order.</td>
              </tr>
            }
            @for (item of o.items; track item.productId) {
              <tr>
                <td class="px-4 py-3 tabular-nums">{{ item.productId }}</td>
                <td class="px-4 py-3 tabular-nums">{{ item.quantity }}</td>
                <td class="px-4 py-3 tabular-nums">{{ item.price | number: '1.2-2' }}</td>
                <td class="px-4 py-3">
                  <div *appHasPermission="permissions.order.update" class="flex justify-end gap-3">
                    <button
                      type="button"
                      class="text-text-muted hover:text-text disabled:opacity-40"
                      [disabled]="isBusy() || o.status !== 'Pending'"
                      (click)="changeQuantity(item.productId, 1)"
                    >
                      +1
                    </button>
                    <button
                      type="button"
                      class="text-text-muted hover:text-text disabled:opacity-40"
                      [disabled]="isBusy() || o.status !== 'Pending' || item.quantity <= 1"
                      (click)="changeQuantity(item.productId, -1)"
                    >
                      −1
                    </button>
                    <button
                      type="button"
                      class="text-danger hover:opacity-80 disabled:opacity-40"
                      [disabled]="isBusy() || o.status !== 'Pending'"
                      (click)="removeItem(item.productId)"
                    >
                      Remove
                    </button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <div *appHasPermission="permissions.order.update" class="app-card mt-4 p-5">
        <h2 class="text-sm font-medium text-text">Add product</h2>
        <form [formGroup]="addForm" (ngSubmit)="addProduct()" class="mt-3 grid grid-cols-1 items-start gap-3 sm:grid-cols-[1fr_1fr_1fr_auto]">
          <app-form-field label="Product ID" [control]="addForm.controls.productId" [required]="true">
            <input formControlName="productId" type="number" min="1" class="app-input" />
          </app-form-field>
          <app-form-field label="Quantity" [control]="addForm.controls.quantity" [required]="true">
            <input formControlName="quantity" type="number" min="1" step="1" class="app-input" />
          </app-form-field>
          <app-form-field label="Unit price" [control]="addForm.controls.price" [required]="true">
            <input formControlName="price" type="number" min="0.01" step="0.01" class="app-input" />
          </app-form-field>
          <button
            app-button
            type="submit"
            variant="secondary"
            class="mt-6"
            [loading]="isBusy()"
            [disabled]="o.status !== 'Pending'"
          >
            Add
          </button>
        </form>
      </div>

      <div class="mt-4 flex justify-end gap-3">
        <button
          *appHasPermission="permissions.order.update"
          app-button
          variant="secondary"
          [loading]="isBusy()"
          [disabled]="o.status !== 'Pending'"
          (click)="cancelOrder()"
        >
          Cancel order
        </button>
        <button
          *appHasPermission="permissions.order.create"
          app-button
          variant="primary"
          [loading]="isBusy()"
          [disabled]="o.status !== 'Pending'"
          (click)="submitOrder()"
        >
          Submit order
        </button>
      </div>
    } @else {
      <div class="app-card mt-4 p-5 text-sm text-text-muted">
        This order isn't in this browser session's memory — probably because the page was reloaded.
        There's no order-lookup endpoint to recover it from; start a new order or use the direct action
        endpoints if you already know the order ID.
      </div>
    }
  `,
})
export class OrderWorkspaceComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ordersService = inject(OrdersService);
  private readonly workspaceStore = inject(OrdersWorkspaceStore);
  private readonly notifications = inject(NotificationService);

  readonly orderId = input.required<string>();

  protected readonly permissions = PERMISSIONS;
  protected readonly isBusy = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected order = () => this.workspaceStore.get(this.orderId());

  protected readonly addForm = this.fb.nonNullable.group({
    productId: [0, [Validators.required, Validators.min(1)]],
    quantity: [1, [Validators.required, quantityPositive()]],
    price: [0, [Validators.required, pricePositive()]],
  });

  protected statusNote(status: string): string {
    return status === 'Submitted'
      ? 'Submitted — payment and shipping are running in the background. There is no way to check progress from this API; the order will be marked Shipped only once every item ships, and this session has no way to observe that.'
      : 'This order was canceled and can no longer be changed.';
  }

  protected addProduct(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const { productId, quantity, price } = this.addForm.getRawValue();
    this.run(
      this.ordersService.addProduct(this.orderId(), { productId, quantity, price: { value: price } }),
      () => {
        this.workspaceStore.addOrIncreaseItem(this.orderId(), { productId, quantity, price });
        this.addForm.reset({ productId: 0, quantity: 1, price: 0 });
      },
    );
  }

  protected changeQuantity(productId: number, delta: number): void {
    const request$ =
      delta > 0
        ? this.ordersService.increaseQuantity(this.orderId(), { productId, quantity: delta })
        : this.ordersService.decreaseQuantity(this.orderId(), { productId, quantity: -delta });

    this.run(request$, () => this.workspaceStore.changeQuantity(this.orderId(), productId, delta));
  }

  protected removeItem(productId: number): void {
    this.run(this.ordersService.removeProduct(this.orderId(), { productId }), () =>
      this.workspaceStore.removeItem(this.orderId(), productId),
    );
  }

  protected submitOrder(): void {
    this.run(this.ordersService.submitOrder(this.orderId()), () => {
      this.workspaceStore.setStatus(this.orderId(), 'Submitted');
      this.notifications.success('Order submitted.');
    });
  }

  protected cancelOrder(): void {
    this.run(this.ordersService.cancelOrder(this.orderId()), () => {
      this.workspaceStore.setStatus(this.orderId(), 'Canceled');
      this.notifications.success('Order canceled.');
    });
  }

  private run(request$: Observable<unknown>, onSuccess: () => void): void {
    this.errorMessage.set(null);
    this.isBusy.set(true);
    request$.pipe(finalize(() => this.isBusy.set(false))).subscribe({
      next: onSuccess,
      error: (error) => this.errorMessage.set(extractErrorMessage(error)),
    });
  }
}
