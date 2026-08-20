import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-warehouse-home',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-lg font-semibold text-text">Warehouse</h1>
    <p class="mt-1 text-sm text-text-muted">
      There's no stock-level read endpoint — these are blind adjustments, not an inventory view.
    </p>

    <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
      <a routerLink="receive" class="app-card block p-5 hover:border-accent transition-colors">
        <h2 class="font-medium text-text">Receive stock</h2>
        <p class="mt-1 text-sm text-text-muted">Record incoming stock for a SKU.</p>
      </a>
      <a routerLink="ship" class="app-card block p-5 hover:border-accent transition-colors">
        <h2 class="font-medium text-text">Ship stock</h2>
        <p class="mt-1 text-sm text-text-muted">Manually ship stock against an order.</p>
      </a>
      <a routerLink="adjust" class="app-card block p-5 hover:border-accent transition-colors">
        <h2 class="font-medium text-text">Adjust stock</h2>
        <p class="mt-1 text-sm text-text-muted">Correct stock with a reason on record.</p>
      </a>
    </div>
  `,
})
export class WarehouseHomeComponent {}
