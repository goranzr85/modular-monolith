import { Routes } from '@angular/router';
import { WarehouseHomeComponent } from './warehouse-home/warehouse-home.component';
import { ReceiveStockComponent } from './receive-stock/receive-stock.component';
import { ShipStockComponent } from './ship-stock/ship-stock.component';
import { AdjustStockComponent } from './adjust-stock/adjust-stock.component';

// Permission (warehouse:update) is already enforced at the parent 'warehouse'
// route in app.routes.ts — every child here shares that same requirement.
export const WAREHOUSE_ROUTES: Routes = [
  { path: '', component: WarehouseHomeComponent },
  { path: 'receive', component: ReceiveStockComponent },
  { path: 'ship', component: ShipStockComponent },
  { path: 'adjust', component: AdjustStockComponent },
];
