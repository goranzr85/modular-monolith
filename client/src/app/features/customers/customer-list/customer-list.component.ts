import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomersService } from '../data/customers.service';
import { Customer } from '../data/customers.models';
import { SkeletonRowsComponent } from '../../../shared/ui/skeleton/skeleton-rows.component';
import { PaginationComponent } from '../../../shared/ui/pagination/pagination.component';
import { BannerComponent } from '../../../shared/ui/banner/banner.component';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { PERMISSIONS } from '../../../core/auth/permissions';
import { extractErrorMessage } from '../../../core/models/problem-details';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [
    RouterLink,
    SkeletonRowsComponent,
    PaginationComponent,
    BannerComponent,
    ButtonComponent,
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold text-text">Customers</h1>
      <a *appHasPermission="permissions.customer.create" app-button routerLink="new">New customer</a>
    </div>

    @if (errorMessage(); as msg) {
      <div class="mt-4">
        <app-banner kind="error" [message]="msg" />
      </div>
    }

    <div class="mt-4 flex items-center gap-3">
      <input
        type="search"
        class="app-input max-w-xs"
        placeholder="Filter by name, email, or phone"
        [value]="filterText()"
        (input)="onFilterInput($event)"
      />
    </div>

    <div class="app-card mt-4 overflow-x-auto">
      <table class="w-full min-w-max text-left text-sm">
        <thead class="border-b border-border text-text-muted">
          <tr>
            <th class="px-4 py-3 font-medium">Name</th>
            <th class="px-4 py-3 font-medium">Address</th>
            <th class="px-4 py-3 font-medium">Email</th>
            <th class="px-4 py-3 font-medium">Phone</th>
            <th class="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-border">
          @if (loading()) {
            <app-skeleton-rows [rows]="pageSize()" [cols]="4" />
          } @else if (pagedCustomers().length === 0) {
            <tr>
              <td colspan="5" class="px-4 py-8 text-center text-text-muted">
                @if (filterText()) {
                  No customers match "{{ filterText() }}".
                } @else {
                  No customers yet.
                }
              </td>
            </tr>
          } @else {
            @for (customer of pagedCustomers(); track customer.id) {
              <tr class="hover:bg-bg">
                <td class="px-4 py-3 text-text">
                  {{ customer.firstName }} {{ customer.middleName ? customer.middleName + ' ' : '' }}{{ customer.lastName }}
                </td>
                <td class="px-4 py-3 text-text-muted">{{ customer.street }}, {{ customer.city }}, {{ customer.state }} {{ customer.zip }}</td>
                <td class="px-4 py-3 text-text-muted">{{ customer.email || '—' }}</td>
                <td class="px-4 py-3 text-text-muted">{{ customer.phone || '—' }}</td>
                <td class="px-4 py-3 text-right">
                  <a [routerLink]="[customer.id]" class="text-sm font-medium text-accent hover:text-accent-hover">View</a>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>

      @if (!loading() && filteredCustomers().length > 0) {
        <div class="border-t border-border px-4">
          <app-pagination
            [totalItems]="filteredCustomers().length"
            [pageIndex]="pageIndex()"
            [pageSize]="pageSize()"
            (pageChange)="pageIndex.set($event)"
            (pageSizeChange)="onPageSizeChange($event)"
          />
        </div>
      }
    </div>
  `,
})
export class CustomerListComponent {
  private readonly customersService = inject(CustomersService);

  protected readonly permissions = PERMISSIONS;
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly customers = signal<Customer[]>([]);

  protected readonly filterText = signal('');
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);

  protected readonly filteredCustomers = computed<Customer[]>(() => {
    const term = this.filterText().trim().toLowerCase();
    const all = this.customers();
    if (!term) return all;
    return all.filter((c) =>
      [c.firstName, c.middleName, c.lastName, c.email, c.phone]
        .filter(Boolean)
        .some((field) => field!.toLowerCase().includes(term)),
    );
  });

  protected readonly pagedCustomers = computed<Customer[]>(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.filteredCustomers().slice(start, start + this.pageSize());
  });

  constructor() {
    this.customersService.list().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(extractErrorMessage(error));
        this.loading.set(false);
      },
    });
  }

  protected onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageIndex.set(0);
  }

  protected onFilterInput(event: Event): void {
    this.filterText.set((event.target as HTMLInputElement).value);
    this.pageIndex.set(0);
  }
}
