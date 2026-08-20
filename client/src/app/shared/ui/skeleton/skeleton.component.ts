import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A single shimmering placeholder block. Compose several inline wherever a
 * page is waiting on an API response — e.g.
 *   <app-skeleton width="60%" height="1rem" />
 *   <app-skeleton height="2.5rem" rounded="lg" />
 */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '',
  host: {
    class: 'block animate-pulse bg-skeleton-base',
    '[style.width]': 'width()',
    '[style.height]': 'height()',
    '[class.rounded-sm]': 'rounded() === "sm"',
    '[class.rounded-md]': 'rounded() === "md"',
    '[class.rounded-lg]': 'rounded() === "lg"',
    '[class.rounded-full]': 'rounded() === "full"',
  },
})
export class SkeletonComponent {
  readonly width = input<string>('100%');
  readonly height = input<string>('1rem');
  readonly rounded = input<'sm' | 'md' | 'lg' | 'full'>('md');
}
