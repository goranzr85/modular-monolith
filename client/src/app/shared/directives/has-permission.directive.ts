import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * `*appHasPermission="'catalog:create'"` or `*appHasPermission="['order:create', 'order:update']"`
 * (any-of match). Purely a UX nicety — see docs/frontend-prd.md §3.3: the
 * backend is the real enforcement boundary and returns 403 regardless of
 * what's shown here.
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);

  readonly appHasPermission = input.required<string | string[]>();

  private created = false;

  constructor() {
    effect(() => {
      const required = this.appHasPermission();
      const list = Array.isArray(required) ? required : [required];
      const allowed = this.auth.hasAnyPermission(list);

      if (allowed && !this.created) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.created = true;
      } else if (!allowed && this.created) {
        this.viewContainer.clear();
        this.created = false;
      }
    });
  }
}
