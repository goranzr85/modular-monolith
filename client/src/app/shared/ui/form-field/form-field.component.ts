import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { FieldErrorComponent } from './field-error.component';

let nextId = 0;

/**
 * Label + projected input + inline validation message, so every form field
 * in the app looks and behaves the same. Usage:
 *
 *   <app-form-field label="SKU" [control]="form.controls.sku" [required]="true">
 *     <input appFormFieldControl formControlName="sku" [id]="fieldId" class="app-input" />
 *   </app-form-field>
 *
 * For the label's `for` to point at the right control, grab a template
 * reference and bind the id yourself (content projection can't do this
 * automatically):
 *
 *   <app-form-field #f label="SKU" [control]="form.controls.sku" required>
 *     <input formControlName="sku" [id]="f.fieldId" class="app-input" />
 *   </app-form-field>
 */
@Component({
  selector: 'app-form-field',
  standalone: true,
  exportAs: 'appFormField',
  imports: [FieldErrorComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <label [for]="fieldId" class="app-label">
        {{ label() }}
        @if (required()) {
          <span class="text-danger">*</span>
        }
      </label>
      <ng-content />
      @if (hint(); as h) {
        <p class="mt-1 text-xs text-text-muted">{{ h }}</p>
      }
      <app-field-error [control]="control()" [label]="label()" [forceShow]="forceShow()" />
    </div>
  `,
})
export class FormFieldComponent {
  readonly label = input.required<string>();
  readonly control = input.required<AbstractControl | null>();
  readonly required = input(false);
  readonly hint = input<string | null>(null);
  readonly forceShow = input(false);

  readonly fieldId = `field-${++nextId}`;
}
