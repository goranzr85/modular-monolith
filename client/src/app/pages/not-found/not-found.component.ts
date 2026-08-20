import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-card mx-auto mt-12 max-w-md p-6 text-center">
      <h1 class="text-lg font-semibold text-text">Page not found</h1>
      <p class="mt-2 text-sm text-text-muted">The page you're looking for doesn't exist.</p>
      <a routerLink="/" class="mt-4 inline-block text-sm font-medium text-accent hover:text-accent-hover">
        Back to home
      </a>
    </div>
  `,
})
export class NotFoundComponent {}
