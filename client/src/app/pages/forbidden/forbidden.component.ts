import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-card mx-auto mt-12 max-w-md p-6 text-center">
      <h1 class="text-lg font-semibold text-text">You don't have access to this page</h1>
      <p class="mt-2 text-sm text-text-muted">
        Your account doesn't hold the permission this page requires. Contact an administrator if you
        believe this is a mistake.
      </p>
      <a routerLink="/" class="mt-4 inline-block text-sm font-medium text-accent hover:text-accent-hover">
        Back to home
      </a>
    </div>
  `,
})
export class ForbiddenComponent {}
