import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { WarehouseService } from '../data/warehouse.service';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { quantityPositive } from '../../../shared/validators/custom-validators';

/** There's no stock-read endpoint — this is a blind adjustment, not an
 *  inventory view (docs/frontend-prd.md §2.4). */
@Component({
  selector: 'app-receive-stock',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-lg">
      <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Warehouse</a>
      <h1 class="mt-2 text-lg font-semibold text-text">Receive stock</h1>
      <p class="mt-1 text-sm text-text-muted">
        Increases quantity on hand for a SKU already known to the warehouse (created automatically when
        Catalog creates the product).
      </p>

      @if (message(); as m) {
        <div class="mt-4">
          <app-banner [kind]="m.kind" [message]="m.text" [dismissible]="true" (dismissed)="message.set(null)" />
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="app-card mt-4 flex flex-col gap-4 p-5">
        <app-form-field #skuField label="SKU" [control]="form.controls.sku" [required]="true">
          <input formControlName="sku" [id]="skuField.fieldId" class="app-input" placeholder="WIDGET-001" />
        </app-form-field>
        <app-form-field #qtyField label="Quantity received" [control]="form.controls.quantity" [required]="true">
          <input formControlName="quantity" [id]="qtyField.fieldId" type="number" min="1" step="1" class="app-input" />
        </app-form-field>

        <div class="mt-2 flex justify-end">
          <button app-button type="submit" variant="primary" [loading]="saving()">Receive</button>
        </div>
      </form>
    </div>
  `,
})
export class ReceiveStockComponent {
  private readonly fb = inject(FormBuilder);
  private readonly warehouse = inject(WarehouseService);

  protected readonly saving = signal(false);
  protected readonly message = signal<{ kind: 'error' | 'success'; text: string } | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    sku: ['', [Validators.required]],
    quantity: [1, [Validators.required, quantityPositive()]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.message.set(null);
    this.saving.set(true);

    const { sku, quantity } = this.form.getRawValue();

    this.warehouse
      .receive(sku, { quantity })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.message.set({ kind: 'success', text: `Received ${quantity} of ${sku}.` });
          this.form.reset({ sku: '', quantity: 1 });
        },
        error: (error) => this.message.set({ kind: 'error', text: extractErrorMessage(error) }),
      });
  }
}
