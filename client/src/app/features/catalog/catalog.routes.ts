import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { PERMISSIONS } from '../../core/auth/permissions';
import { CatalogHomeComponent } from './catalog-home/catalog-home.component';
import { CreateProductComponent } from './create-product/create-product.component';
import { EditProductComponent } from './edit-product/edit-product.component';

export const CATALOG_ROUTES: Routes = [
  { path: '', component: CatalogHomeComponent },
  {
    path: 'create',
    component: CreateProductComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.catalog.create },
  },
  {
    path: 'edit',
    component: EditProductComponent,
    canActivate: [authGuard],
    data: { permission: PERMISSIONS.catalog.update },
  },
];
