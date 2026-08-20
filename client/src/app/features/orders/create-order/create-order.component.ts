import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { OrdersService } from '../data/orders.service';
import { OrdersWorkspaceStore } from '../data/orders-workspace.store';
import { CustomersService } from '../../customers/data/customers.service';
import { Customer } from '../../customers/data/customers.models';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton/skeleton.component';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { quantityPositive, pricePositive } from '../../../shared/validators/custom-validators';

@Component({
  selector: 'app-create-order',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent, SkeletonComponent, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-3xl">
      <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Orders</a>
      <h1 class="mt-2 text-lg font-semibold text-text">New order</h1>

      @if (errorMessage(); as msg) {
        <div class="mt-4">
          <app-banner kind="error" [message]="msg" [dismissible]="true" (dismissed)="errorMessage.set(null)" />
        </div>
      }

      <div class="app-card mt-4 p-5">
        <label class="app-label">Customer</label>
        @if (selectedCustomer(); as c) {
          <div class="flex items-center justify-between rounded-md border border-border-strong bg-bg px-3 py-2 text-sm">
            <span>{{ c.firstName }} {{ c.lastName }} &middot; {{ c.email || c.phone || 'no contact on file' }}</span>
            <button type="button" class="text-accent hover:text-accent-hover" (click)="clearCustomer()">Change</button>
          </div>
        } @else if (loadingCustomers()) {
          <div class="flex flex-col gap-2">
            <app-skeleton height="2.25rem" rounded="md" />
            <p class="text-xs text-text-muted">Loading customers…</p>
          </div>
        } @else {
          <input
            type="search"
            class="app-input"
            placeholder="Search customers by name or email"
            [value]="customerQuery()"
            (input)="onCustomerQuery($event)"
          />
          @if (customerQuery() && filteredCustomers().length > 0) {
            <ul class="mt-1 max-h-48 overflow-y-auto rounded-md border border-border bg-surface-raised shadow-sm">
              @for (c of filteredCustomers(); track c.id) {
                <li>
                  <button
                    type="button"
                    class="block w-full px-3 py-2 text-left text-sm hover:bg-bg"
                    (click)="selectCustomer(c)"
                  >
                    {{ c.firstName }} {{ c.lastName }} &middot; {{ c.email || c.phone || 'no contact on file' }}
                  </button>
                </li>
              }
            </ul>
          } @else if (customerQuery()) {
            <p class="mt-1 text-xs text-text-muted">No customers match.</p>
          }
        }
        @if (customerRequiredError()) {
          <p class="mt-1 text-xs text-danger">Select a customer.</p>
        }
      </div>

      <div class="app-card mt-4 p-5">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-medium text-text">Line items</h2>
          <button type="button" class="text-sm font-medium text-accent hover:text-accent-hover" (click)="addItem()">
            + Add item
          </button>
        </div>

        <form [formGroup]="form">
          <div formArrayName="items" class="mt-3 flex flex-col gap-3">
            @for (item of itemsArray.controls; track item; let i = $index) {
              <div [formGroupName]="i" class="grid grid-cols-1 items-start gap-3 sm:grid-cols-[1fr_1fr_1fr_auto]">
                <app-form-field label="Product ID" [control]="item.controls.productId" [required]="true">
                  <input formControlName="productId" type="number" min="1" class="app-input" />
                </app-form-field>
                <app-form-field label="Quantity" [control]="item.controls.quantity" [required]="true">
                  <input formControlName="quantity" type="number" min="1" step="1" class="app-input" />
                </app-form-field>
                <app-form-field label="Unit price" [control]="item.controls.price" [required]="true">
                  <input formControlName="price" type="number" min="0.01" step="0.01" class="app-input" />
                </app-form-field>
                <button
                  type="button"
                  class="mt-6 h-fit text-sm text-danger hover:opacity-80"
                  [disabled]="itemsArray.length === 1"
                  (click)="removeItem(i)"
                >
                  Remove
                </button>
              </div>
            }
          </div>
        </form>

        <div class="mt-4 flex justify-end border-t border-border pt-3 text-sm">
          <span class="text-text-muted">Total:&nbsp;</span>
          <span class="font-medium tabular-nums text-text">{{ total() | number: '1.2-2' }}</span>
        </div>
      </div>

      <div class="mt-4 flex justify-end gap-3">
        <button app-button type="button" variant="secondary" routerLink="..">Cancel</button>
        <button app-button type="button" variant="primary" [loading]="saving()" (click)="submit()">
          Create order
        </button>
      </div>
    </div>
  `,
})
export class CreateOrderComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ordersService = inject(OrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly workspaceStore = inject(OrdersWorkspaceStore);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly customerRequiredError = signal(false);

  protected readonly allCustomers = signal<Customer[]>([]);
  protected readonly loadingCustomers = signal(true);
  protected readonly customerQuery = signal('');
  protected readonly selectedCustomer = signal<Customer | null>(null);

  protected readonly filteredCustomers = computed<Customer[]>(() => {
    const term = this.customerQuery().trim().toLowerCase();
    if (!term) return [];
    return this.allCustomers()
      .filter((c) => `${c.firstName} ${c.lastName} ${c.email ?? ''}`.toLowerCase().includes(term))
      .slice(0, 8);
  });

  protected readonly form = this.fb.nonNullable.group({
    items: this.fb.array([this.buildItemGroup()]),
  });

  protected get itemsArray() {
    return this.form.controls.items;
  }

  private readonly itemValues = toSignal(this.itemsArray.valueChanges, { initialValue: this.itemsArray.getRawValue() });

  protected readonly total = computed(() =>
    this.itemValues().reduce((sum, item) => sum + (item.quantity ?? 0) * (item.price ?? 0), 0),
  );

  constructor() {
    this.customersService.list().subscribe({
      next: (customers) => {
        this.allCustomers.set(customers);
        this.loadingCustomers.set(false);
      },
      error: (error) => {
        this.errorMessage.set(extractErrorMessage(error));
        this.loadingCustomers.set(false);
      },
    });
  }

  private buildItemGroup() {
    return this.fb.nonNullable.group({
      productId: [0, [Validators.required, Validators.min(1)]],
      quantity: [1, [Validators.required, quantityPositive()]],
      price: [0, [Validators.required, pricePositive()]],
    });
  }

  protected addItem(): void {
    this.itemsArray.push(this.buildItemGroup());
  }

  protected removeItem(index: number): void {
    if (this.itemsArray.length > 1) {
      this.itemsArray.removeAt(index);
    }
  }

  protected onCustomerQuery(event: Event): void {
    this.customerQuery.set((event.target as HTMLInputElement).value);
  }

  protected selectCustomer(customer: Customer): void {
    this.selectedCustomer.set(customer);
    this.customerRequiredError.set(false);
    this.customerQuery.set('');
  }

  protected clearCustomer(): void {
    this.selectedCustomer.set(null);
  }

  protected submit(): void {
    const customer = this.selectedCustomer();
    if (!customer) {
      this.customerRequiredError.set(true);
    }
    if (this.form.invalid || !customer) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.saving.set(true);

    const items = this.itemsArray.getRawValue();

    this.ordersService
      .createOrder({
        customerId: customer.id,
        items: items.map((i) => ({ productId: i.productId, quantity: i.quantity, price: { value: i.price } })),
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (orderId) => {
          this.workspaceStore.create(
            orderId,
            customer.id,
            `${customer.firstName} ${customer.lastName}`,
            items.map((i) => ({ productId: i.productId, quantity: i.quantity, price: i.price })),
          );
          this.router.navigate(['/orders/workspace', orderId]);
        },
        error: (error) => this.errorMessage.set(extractErrorMessage(error)),
      });
  }
}
