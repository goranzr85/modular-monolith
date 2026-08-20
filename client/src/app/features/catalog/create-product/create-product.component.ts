import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CatalogService } from '../data/catalog.service';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { NotificationService } from '../../../core/services/notification.service';
import { priceNonNegative } from '../../../shared/validators/custom-validators';

@Component({
  selector: 'app-create-product',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-xl">
      <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Catalog</a>
      <h1 class="mt-2 text-lg font-semibold text-text">New product</h1>

      @if (errorMessage(); as msg) {
        <div class="mt-4">
          <app-banner kind="error" [message]="msg" [dismissible]="true" (dismissed)="errorMessage.set(null)" />
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="app-card mt-4 flex flex-col gap-4 p-5">
        <app-form-field #skuField label="SKU" [control]="form.controls.sku" [required]="true">
          <input
            formControlName="sku"
            [id]="skuField.fieldId"
            maxlength="15"
            class="app-input"
            placeholder="WIDGET-001"
          />
        </app-form-field>

        <app-form-field #nameField label="Name" [control]="form.controls.name" [required]="true">
          <input formControlName="name" [id]="nameField.fieldId" maxlength="50" class="app-input" />
        </app-form-field>

        <app-form-field #descField label="Description" [control]="form.controls.description" [required]="true">
          <textarea formControlName="description" [id]="descField.fieldId" rows="3" class="app-input"></textarea>
        </app-form-field>

        <app-form-field
          #priceField
          label="Price"
          [control]="form.controls.price"
          [required]="true"
          hint="Zero is allowed on create; the backend rejects negative values."
        >
          <input
            formControlName="price"
            [id]="priceField.fieldId"
            type="number"
            step="0.01"
            min="0"
            class="app-input"
          />
        </app-form-field>

        <div class="mt-2 flex justify-end gap-3">
          <button app-button type="button" variant="secondary" routerLink="..">Cancel</button>
          <button app-button type="submit" variant="primary" [loading]="saving()" [disabled]="form.invalid">
            Create product
          </button>
        </div>
      </form>
    </div>
  `,
})
export class CreateProductComponent {
  private readonly fb = inject(FormBuilder);
  private readonly catalog = inject(CatalogService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    sku: ['', [Validators.required, Validators.maxLength(15)]],
    name: ['', [Validators.required, Validators.maxLength(50)]],
    description: ['', [Validators.required]],
    price: [0, [Validators.required, priceNonNegative()]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.saving.set(true);

    const { sku, name, description, price } = this.form.getRawValue();

    this.catalog
      .createProduct({ sku, name, description, price })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success(`Product ${sku} created.`);
          this.router.navigateByUrl('/catalog');
        },
        error: (error) => this.errorMessage.set(extractErrorMessage(error)),
      });
  }
}
