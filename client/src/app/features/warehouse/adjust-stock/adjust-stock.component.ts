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

type Direction = 'increase' | 'decrease';

/** The one place the backend requires a reason — e.g. a stocktake
 *  correction or damage write-off (docs/frontend-prd.md §2.4.3). */
@Component({
  selector: 'app-adjust-stock',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-lg">
      <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Warehouse</a>
      <h1 class="mt-2 text-lg font-semibold text-text">Adjust stock</h1>
      <p class="mt-1 text-sm text-text-muted">Manual correction, e.g. a stocktake or a damage write-off.</p>

      @if (message(); as m) {
        <div class="mt-4">
          <app-banner [kind]="m.kind" [message]="m.text" [dismissible]="true" (dismissed)="message.set(null)" />
          @if (canRetry()) {
            <button app-button type="button" variant="secondary" class="mt-2" (click)="submit()">Retry</button>
          }
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="app-card mt-4 flex flex-col gap-4 p-5">
        <div>
          <span class="app-label">Direction</span>
          <div class="flex gap-4">
            <label class="flex items-center gap-2 text-sm text-text">
              <input type="radio" formControlName="direction" value="increase" />
              Increase
            </label>
            <label class="flex items-center gap-2 text-sm text-text">
              <input type="radio" formControlName="direction" value="decrease" />
              Decrease
            </label>
          </div>
        </div>

        <app-form-field #skuField label="SKU" [control]="form.controls.sku" [required]="true">
          <input formControlName="sku" [id]="skuField.fieldId" class="app-input" placeholder="WIDGET-001" />
        </app-form-field>
        <app-form-field #qtyField label="Quantity" [control]="form.controls.quantity" [required]="true">
          <input formControlName="quantity" [id]="qtyField.fieldId" type="number" min="1" step="1" class="app-input" />
        </app-form-field>
        <app-form-field #reasonField label="Reason" [control]="form.controls.reason" [required]="true">
          <input formControlName="reason" [id]="reasonField.fieldId" class="app-input" placeholder="Stocktake correction" />
        </app-form-field>

        <div class="mt-2 flex justify-end">
          <button app-button type="submit" variant="primary" [loading]="saving()">
            {{ form.controls.direction.value === 'increase' ? 'Increase' : 'Decrease' }}
          </button>
        </div>
      </form>
    </div>
  `,
})
export class AdjustStockComponent {
  private readonly fb = inject(FormBuilder);
  private readonly warehouse = inject(WarehouseService);

  protected readonly saving = signal(false);
  protected readonly message = signal<{ kind: 'error' | 'success'; text: string } | null>(null);
  protected readonly canRetry = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    direction: ['increase' as Direction, [Validators.required]],
    sku: ['', [Validators.required]],
    quantity: [1, [Validators.required, quantityPositive()]],
    reason: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.message.set(null);
    this.canRetry.set(false);
    this.saving.set(true);

    const { direction, sku, quantity, reason } = this.form.getRawValue();
    const request$ =
      direction === 'increase'
        ? this.warehouse.increase(sku, { quantity, reason })
        : this.warehouse.decrease(sku, { quantity, reason });

    request$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.message.set({
          kind: 'success',
          text: `${direction === 'increase' ? 'Increased' : 'Decreased'} ${sku} by ${quantity}.`,
        });
        this.form.reset({ direction, sku: '', quantity: 1, reason: '' });
      },
      error: (error) => {
        this.message.set({ kind: 'error', text: extractErrorMessage(error) });
        this.canRetry.set(error instanceof HttpErrorResponse && error.status === 409);
      },
    });
  }
}
