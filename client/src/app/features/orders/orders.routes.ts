import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { PERMISSIONS } from '../../core/auth/permissions';
import { OrdersHomeComponent } from './orders-home/orders-home.component';
import { CreateOrderComponent } from './create-order/create-order.component';
import { OrderWorkspaceComponent } from './order-workspace/order-workspace.component';

export const ORDERS_ROUTES: Routes = [
  { path: '', component: OrdersHomeComponent },
  {
    path: 'new',
    component: CreateOrderComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.order.create },
  },
  // Both order:create (needed for Submit — a confirmed backend permission
  // inconsistency) and order:update gate individual actions inside the
  // workspace itself; the route only requires the broader, more common one.
  {
    path: 'workspace/:orderId',
    component: OrderWorkspaceComponent,
    canActivate: [authGuard],
    data: { permission: [PERMISSIONS.order.update, PERMISSIONS.order.create] },
  },
];
