import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomersService } from '../data/customers.service';
import { Customer } from '../data/customers.models';
import { SkeletonComponent } from '../../../shared/ui/skeleton/skeleton.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { PERMISSIONS } from '../../../core/auth/permissions';
import { extractErrorMessage } from '../../../core/models/problem-details';

/** Any authenticated user can view a customer — the backend requires no
 *  permission here at all, unlike the list endpoint (docs/frontend-prd.md §2.2.2). */
@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [RouterLink, SkeletonComponent, BannerComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink=".." class="text-sm text-text-muted hover:text-text">&larr; Customers</a>

    @if (errorMessage(); as msg) {
      <div class="mt-4">
        <app-banner kind="error" [message]="msg" />
      </div>
    } @else if (loading()) {
      <div class="app-card mt-4 flex flex-col gap-3 p-5">
        <app-skeleton width="40%" height="1.25rem" />
        <app-skeleton width="60%" />
        <app-skeleton width="50%" />
        <app-skeleton width="30%" />
      </div>
    } @else if (customer(); as c) {
      <div class="mt-4 flex items-start justify-between">
        <div>
          <h1 class="text-lg font-semibold text-text">
            {{ c.firstName }} {{ c.middleName ? c.middleName + ' ' : '' }}{{ c.lastName }}
          </h1>
        </div>
        <a
          *appHasPermission="permissions.customer.update"
          routerLink="../{{ c.id }}/edit"
          class="text-sm font-medium text-accent hover:text-accent-hover"
        >
          Edit
        </a>
      </div>

      <dl class="app-card mt-4 grid grid-cols-1 gap-x-6 gap-y-4 p-5 sm:grid-cols-2">
        <div>
          <dt class="text-xs font-medium uppercase tracking-wide text-text-faint">Billing address</dt>
          <dd class="mt-1 text-sm text-text">{{ c.street }}, {{ c.city }}, {{ c.state }} {{ c.zip }}</dd>
        </div>
        <div>
          <dt class="text-xs font-medium uppercase tracking-wide text-text-faint">Email</dt>
          <dd class="mt-1 text-sm text-text">{{ c.email || '—' }}</dd>
        </div>
        <div>
          <dt class="text-xs font-medium uppercase tracking-wide text-text-faint">Phone</dt>
          <dd class="mt-1 text-sm text-text">{{ c.phone || '—' }}</dd>
        </div>
      </dl>
      <p class="mt-3 text-xs text-text-faint">
        Shipping address isn't returned by any endpoint on this API, so it can't be shown here.
      </p>
    }
  `,
})
export class CustomerDetailComponent {
  private readonly customersService = inject(CustomersService);

  /** Bound automatically from the `:id` route param via withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly permissions = PERMISSIONS;
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly customer = signal<Customer | null>(null);

  constructor() {
    effect(() => {
      this.loading.set(true);
      this.customersService.getById(this.id()).subscribe({
        next: (customer) => {
          this.customer.set(customer);
          this.loading.set(false);
        },
        error: (error) => {
          this.errorMessage.set(extractErrorMessage(error));
          this.loading.set(false);
        },
      });
    });
  }
}
