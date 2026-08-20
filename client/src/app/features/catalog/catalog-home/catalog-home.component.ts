import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PERMISSIONS } from '../../../core/auth/permissions';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

/**
 * There is no GET /api/products anywhere on the backend, so there's no
 * product list to land on here — just entry points into the two write
 * actions that actually exist (docs/frontend-prd.md §2.1.3).
 */
@Component({
  selector: 'app-catalog-home',
  standalone: true,
  imports: [RouterLink, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-lg font-semibold text-text">Catalog</h1>
    <p class="mt-1 text-sm text-text-muted">
      There's no product list in this API yet — create or edit a product by SKU below.
    </p>

    <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
      <a
        *appHasPermission="permissions.catalog.create"
        routerLink="create"
        class="app-card block p-5 hover:border-accent transition-colors"
      >
        <h2 class="font-medium text-text">New product</h2>
        <p class="mt-1 text-sm text-text-muted">Add a product to the catalog.</p>
      </a>
      <a
        *appHasPermission="permissions.catalog.update"
        routerLink="edit"
        class="app-card block p-5 hover:border-accent transition-colors"
      >
        <h2 class="font-medium text-text">Edit product</h2>
        <p class="mt-1 text-sm text-text-muted">Update an existing product by SKU.</p>
      </a>
    </div>
  `,
})
export class CatalogHomeComponent {
  protected readonly permissions = PERMISSIONS;
}
