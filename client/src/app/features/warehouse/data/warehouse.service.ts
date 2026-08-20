import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdjustStockRequest, ReceiveStockRequest, ShipStockRequest } from './warehouse.models';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/warehouse`;

  receive(sku: string, request: ReceiveStockRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/received/${encodeURIComponent(sku)}`, request);
  }

  ship(sku: string, request: ShipStockRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/shipping/${encodeURIComponent(sku)}`, request);
  }

  increase(sku: string, request: AdjustStockRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/increased/${encodeURIComponent(sku)}`, request);
  }

  decrease(sku: string, request: AdjustStockRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/decreased/${encodeURIComponent(sku)}`, request);
  }
}
