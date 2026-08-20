import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProductRequest } from './catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;

  /**
   * 201 Created, but the response body/Location are unusable — the backend
   * returns Unit, not the SKU (docs/frontend-prd.md §2.1.1). Callers should
   * ignore the resolved value and use the SKU they already submitted.
   */
  createProduct(request: ProductRequest): Observable<unknown> {
    return this.http.post(this.baseUrl, request);
  }

  /** 200 OK, empty body. */
  updateProduct(request: ProductRequest): Observable<unknown> {
    return this.http.put(this.baseUrl, request);
  }
}
