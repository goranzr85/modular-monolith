import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { PERMISSIONS } from '../../core/auth/permissions';
import { CustomerListComponent } from './customer-list/customer-list.component';
import { CustomerDetailComponent } from './customer-detail/customer-detail.component';
import { CustomerFormComponent } from './customer-form/customer-form.component';

export const CUSTOMERS_ROUTES: Routes = [
  {
    path: '',
    component: CustomerListComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.customer.view },
  },
  // Must precede ':id' — otherwise the router matches "new" as an :id value.
  {
    path: 'new',
    component: CustomerFormComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.customer.create },
  },
  // No permission required — matches the backend's GET /api/customers/id, which is auth-only.
  { path: ':id', component: CustomerDetailComponent, canActivate: [authGuard] },
  {
    path: ':id/edit',
    component: CustomerFormComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.customer.update },
  },
];
