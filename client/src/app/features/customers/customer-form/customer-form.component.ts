import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CustomersService } from '../data/customers.service';
import { CustomerRequest, PrimaryContactType } from '../data/customers.models';
import { FormFieldComponent } from '../../../shared/ui/form-field/form-field.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton/skeleton.component';
import { extractErrorMessage } from '../../../core/models/problem-details';
import { NotificationService } from '../../../core/services/notification.service';
import { customerContactValidator } from '../../../shared/validators/custom-validators';
import { getValidationMessage } from '../../../shared/validators/validation-messages';

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, ButtonComponent, BannerComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-2xl">
      <a routerLink="/customers" class="text-sm text-text-muted hover:text-text">&larr; Customers</a>
      <h1 class="mt-2 text-lg font-semibold text-text">{{ isEdit() ? 'Edit customer' : 'New customer' }}</h1>

      @if (errorMessage(); as msg) {
        <div class="mt-4">
          <app-banner kind="error" [message]="msg" [dismissible]="true" (dismissed)="errorMessage.set(null)" />
        </div>
      }

      @if (loadingExisting()) {
        <div class="app-card mt-4 flex flex-col gap-3 p-5">
          <app-skeleton width="50%" />
          <app-skeleton width="80%" />
          <app-skeleton width="70%" />
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" class="app-card mt-4 flex flex-col gap-5 p-5">
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <app-form-field #firstField label="First name" [control]="form.controls.firstName" [required]="true">
              <input formControlName="firstName" [id]="firstField.fieldId" maxlength="50" class="app-input" />
            </app-form-field>
            <app-form-field #middleField label="Middle name" [control]="form.controls.middleName">
              <input formControlName="middleName" [id]="middleField.fieldId" maxlength="50" class="app-input" />
            </app-form-field>
            <app-form-field #lastField label="Last name" [control]="form.controls.lastName" [required]="true">
              <input formControlName="lastName" [id]="lastField.fieldId" maxlength="50" class="app-input" />
            </app-form-field>
          </div>

          <fieldset class="border-t border-border pt-4" formGroupName="address">
            <legend class="mb-3 text-sm font-medium text-text">Billing address</legend>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <app-form-field #streetField label="Street" [control]="form.controls.address.controls.street" [required]="true">
                <input formControlName="street" [id]="streetField.fieldId" maxlength="100" class="app-input" />
              </app-form-field>
              <app-form-field #cityField label="City" [control]="form.controls.address.controls.city" [required]="true">
                <input formControlName="city" [id]="cityField.fieldId" maxlength="50" class="app-input" />
              </app-form-field>
              <app-form-field #stateField label="State" [control]="form.controls.address.controls.state" [required]="true">
                <input formControlName="state" [id]="stateField.fieldId" maxlength="50" class="app-input" />
              </app-form-field>
              <app-form-field #zipField label="ZIP" [control]="form.controls.address.controls.zip" [required]="true">
                <input formControlName="zip" [id]="zipField.fieldId" maxlength="10" class="app-input" />
              </app-form-field>
            </div>
          </fieldset>

          <div class="border-t border-border pt-4">
            <label class="flex items-center gap-2 text-sm text-text">
              <input type="checkbox" formControlName="shipToDifferentAddress" class="size-4 rounded border-border-strong" />
              Ship to a different address
            </label>
            @if (isEdit()) {
              <p class="mt-1 text-xs text-text-faint">
                This API never returns the current shipping address, so it isn't shown here. Leave this
                unchecked to reset shipping to match billing, or check it and re-enter it to change it.
              </p>
            }

            @if (form.controls.shipToDifferentAddress.value) {
              <fieldset class="mt-3" formGroupName="shippingAddress">
                <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <app-form-field #shipStreetField label="Street" [control]="form.controls.shippingAddress.controls.street" [required]="true">
                    <input formControlName="street" [id]="shipStreetField.fieldId" maxlength="100" class="app-input" />
                  </app-form-field>
                  <app-form-field #shipCityField label="City" [control]="form.controls.shippingAddress.controls.city" [required]="true">
                    <input formControlName="city" [id]="shipCityField.fieldId" maxlength="50" class="app-input" />
                  </app-form-field>
                  <app-form-field #shipStateField label="State" [control]="form.controls.shippingAddress.controls.state" [required]="true">
                    <input formControlName="state" [id]="shipStateField.fieldId" maxlength="50" class="app-input" />
                  </app-form-field>
                  <app-form-field #shipZipField label="ZIP" [control]="form.controls.shippingAddress.controls.zip" [required]="true">
                    <input formControlName="zip" [id]="shipZipField.fieldId" maxlength="10" class="app-input" />
                  </app-form-field>
                </div>
              </fieldset>
            }
          </div>

          <div class="border-t border-border pt-4">
            <h2 class="mb-3 text-sm font-medium text-text">Contact</h2>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <app-form-field #emailField label="Email" [control]="form.controls.email">
                <input formControlName="email" [id]="emailField.fieldId" type="email" maxlength="80" class="app-input" />
              </app-form-field>
              <app-form-field #phoneField label="Phone" [control]="form.controls.phone">
                <input formControlName="phone" [id]="phoneField.fieldId" maxlength="50" class="app-input" />
              </app-form-field>
            </div>

            <fieldset class="mt-3">
              <legend class="app-label">Primary contact method</legend>
              <div class="flex gap-4">
                <label class="flex items-center gap-2 text-sm text-text">
                  <input type="radio" formControlName="primaryContactType" [value]="0" />
                  Email
                </label>
                <label class="flex items-center gap-2 text-sm text-text">
                  <input type="radio" formControlName="primaryContactType" [value]="1" />
                  Phone
                </label>
              </div>
            </fieldset>

            @if (contactGroupError(); as msg) {
              <p class="mt-2 text-xs text-danger">{{ msg }}</p>
            }
          </div>

          <div class="mt-2 flex justify-end gap-3 border-t border-border pt-4">
            <button app-button type="button" variant="secondary" routerLink="/customers">Cancel</button>
            <button app-button type="submit" variant="primary" [loading]="saving()">
              {{ isEdit() ? 'Save changes' : 'Register customer' }}
            </button>
          </div>
        </form>
      }
    </div>
  `,
})
export class CustomerFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly customersService = inject(CustomersService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Present only on the edit route (see customers.routes.ts). */
  readonly id = input<string | undefined>(undefined);

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly saving = signal(false);
  protected readonly loadingExisting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  private readonly submitAttempted = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      middleName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      address: this.fb.nonNullable.group({
        street: ['', [Validators.required, Validators.maxLength(100)]],
        city: ['', [Validators.required, Validators.maxLength(50)]],
        state: ['', [Validators.required, Validators.maxLength(50)]],
        zip: ['', [Validators.required, Validators.maxLength(10)]],
      }),
      shipToDifferentAddress: [false],
      shippingAddress: this.fb.nonNullable.group({
        street: ['', [Validators.maxLength(100)]],
        city: ['', [Validators.maxLength(50)]],
        state: ['', [Validators.maxLength(50)]],
        zip: ['', [Validators.maxLength(10)]],
      }),
      email: ['', [Validators.email, Validators.maxLength(80)]],
      phone: ['', [Validators.maxLength(50)]],
      primaryContactType: [PrimaryContactType.Email as PrimaryContactType],
    },
    { validators: customerContactValidator() },
  );

  protected contactGroupError = computed<string | null>(() => {
    const email = this.form.controls.email;
    const phone = this.form.controls.phone;
    const attempted = this.submitAttempted();
    if (!attempted && !email.touched && !phone.touched) return null;
    return getValidationMessage(this.form.errors, 'Contact');
  });

  constructor() {
    effect(() => {
      const id = this.id();
      if (!id) return;

      this.loadingExisting.set(true);
      this.customersService.getById(id).subscribe({
        next: (customer) => {
          this.form.patchValue({
            firstName: customer.firstName,
            middleName: customer.middleName ?? '',
            lastName: customer.lastName,
            address: {
              street: customer.street,
              city: customer.city,
              state: customer.state,
              zip: customer.zip,
            },
            email: customer.email ?? '',
            phone: customer.phone ?? '',
            primaryContactType: customer.phone && !customer.email ? PrimaryContactType.Phone : PrimaryContactType.Email,
          });
          this.loadingExisting.set(false);
        },
        error: (error) => {
          this.errorMessage.set(extractErrorMessage(error));
          this.loadingExisting.set(false);
        },
      });
    });
  }

  protected submit(): void {
    this.submitAttempted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.saving.set(true);

    const value = this.form.getRawValue();
    const request: CustomerRequest = {
      firstName: value.firstName,
      middleName: emptyToNull(value.middleName),
      lastName: value.lastName,
      address: value.address,
      shippingAddress: value.shipToDifferentAddress ? value.shippingAddress : null,
      email: emptyToNull(value.email),
      phone: emptyToNull(value.phone),
      primaryContactType: value.primaryContactType,
    };

    const id = this.id();
    const request$ = id ? this.customersService.update(id, request) : this.customersService.create(request);

    request$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (response) => {
        this.notifications.success(id ? 'Customer updated.' : 'Customer registered.');
        const newId = id ?? (response as { customerId: string }).customerId;
        this.router.navigate(['/customers', newId]);
      },
      error: (error) => this.errorMessage.set(extractErrorMessage(error)),
    });
  }
}
