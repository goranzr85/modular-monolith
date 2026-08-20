import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

/** A stack of skeleton rows, sized for a table body waiting on data. */
@Component({
  selector: 'app-skeleton-rows',
  standalone: true,
  imports: [SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (row of rowsArray(); track row) {
      <tr>
        @for (col of colsArray(); track col) {
          <td class="px-4 py-3">
            <app-skeleton height="0.9rem" [width]="col === 0 ? '70%' : '90%'" />
          </td>
        }
      </tr>
    }
  `,
})
export class SkeletonRowsComponent {
  readonly rows = input(5);
  readonly cols = input(4);

  protected rowsArray = () => Array.from({ length: this.rows() }, (_, i) => i);
  protected colsArray = () => Array.from({ length: this.cols() }, (_, i) => i);
}
