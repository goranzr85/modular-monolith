import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Client-side paginator — the only list endpoint in this backend
 * (GET /api/customers) returns a bare, unpaginated array, so paging
 * happens entirely in the browser over data already fetched in full.
 * See docs/frontend-prd.md §4.
 */
@Component({
  selector: 'app-pagination',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-center justify-between gap-3 py-3 text-sm text-text-muted">
      <span>
        Showing {{ rangeStart() }}–{{ rangeEnd() }} of {{ totalItems() }}
      </span>

      <div class="flex items-center gap-3">
        <label class="flex items-center gap-2">
          <span>Rows per page</span>
          <select
            class="app-input w-auto py-1"
            [value]="pageSize()"
            (change)="onPageSizeChange($event)"
          >
            @for (size of pageSizeOptions(); track size) {
              <option [value]="size">{{ size }}</option>
            }
          </select>
        </label>

        <div class="flex items-center gap-1">
          <button
            type="button"
            class="app-input w-auto px-2 py-1 disabled:opacity-40"
            [disabled]="pageIndex() === 0"
            (click)="pageChange.emit(pageIndex() - 1)"
            aria-label="Previous page"
          >
            ‹
          </button>
          <span class="px-2 tabular-nums">{{ pageIndex() + 1 }} / {{ totalPages() }}</span>
          <button
            type="button"
            class="app-input w-auto px-2 py-1 disabled:opacity-40"
            [disabled]="pageIndex() >= totalPages() - 1"
            (click)="pageChange.emit(pageIndex() + 1)"
            aria-label="Next page"
          >
            ›
          </button>
        </div>
      </div>
    </div>
  `,
})
export class PaginationComponent {
  readonly totalItems = input.required<number>();
  readonly pageIndex = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly pageSizeOptions = input<number[]>([10, 25, 50, 100]);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected totalPages = computed(() => Math.max(1, Math.ceil(this.totalItems() / this.pageSize())));
  protected rangeStart = computed(() => (this.totalItems() === 0 ? 0 : this.pageIndex() * this.pageSize() + 1));
  protected rangeEnd = computed(() => Math.min(this.totalItems(), (this.pageIndex() + 1) * this.pageSize()));

  protected onPageSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
