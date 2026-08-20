import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { WarehouseService } from '../data/warehouse.service';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { quantityPositive } from '../../../shared/validators/custom-validators';

/** Normally driven automatically by the order-fulfillment saga — a manual
 *  Ship Stock form is really an operator override path (docs/frontend-prd.md §2.4.2). */
@Component({
  selector: 'app-ship-stock',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-lg">
      <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Warehouse</a>
      <h1 class="mt-2 text-lg font-semibold text-text">Ship stock</h1>
      <p class="mt-1 text-sm text-text-muted">
        Manually decreases quantity on hand and records it against an order. Fulfillment normally does
        this automatically after payment succeeds.
      </p>

      @if (message(); as m) {
        <div class="mt-4">
          <app-banner [kind]="m.kind" [message]="m.text" [dismissible]="true" (dismissed)="message.set(null)" />
          @if (canRetry()) {
            <button app-button type="button" variant="secondary" class="mt-2" (click)="submit()">Retry</button>
          }
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="app-card mt-4 flex flex-col gap-4 p-5">
        <app-form-field #skuField label="SKU" [control]="form.controls.sku" [required]="true">
          <input formControlName="sku" [id]="skuField.fieldId" class="app-input" placeholder="WIDGET-001" />
        </app-form-field>
        <app-form-field #orderField label="Order ID" [control]="form.controls.orderId" [required]="true">
          <input formControlName="orderId" [id]="orderField.fieldId" class="app-input" placeholder="Order GUID" />
        </app-form-field>
        <app-form-field #qtyField label="Quantity" [control]="form.controls.quantity" [required]="true">
          <input formControlName="quantity" [id]="qtyField.fieldId" type="number" min="1" step="1" class="app-input" />
        </app-form-field>

        <div class="mt-2 flex justify-end">
          <button app-button type="submit" variant="primary" [loading]="saving()">Ship</button>
        </div>
      </form>
    </div>
  `,
})
export class ShipStockComponent {
  private readonly fb = inject(FormBuilder);
  private readonly warehouse = inject(WarehouseService);

  protected readonly saving = signal(false);
  protected readonly message = signal<{ kind: 'error' | 'success'; text: string } | null>(null);
  protected readonly canRetry = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    sku: ['', [Validators.required]],
    orderId: ['', [Validators.required]],
    quantity: [1, [Validators.required, quantityPositive()]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.message.set(null);
    this.canRetry.set(false);
    this.saving.set(true);

    const { sku, orderId, quantity } = this.form.getRawValue();

    this.warehouse
      .ship(sku, { orderId, quantity })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.message.set({ kind: 'success', text: `Shipped ${quantity} of ${sku} against order ${orderId}.` });
          this.form.reset({ sku: '', orderId: '', quantity: 1 });
        },
        error: (error) => {
          this.message.set({ kind: 'error', text: extractErrorMessage(error) });
          this.canRetry.set(error instanceof HttpErrorResponse && error.status === 409);
        },
      });
  }
}
