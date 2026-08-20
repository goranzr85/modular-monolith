import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AddProductRequest, CreateOrderRequest, ProductQuantityRequest, RemoveProductRequest } from './orders.models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/orders`;

  /** 201 Created — body is the new order's raw GUID string. */
  createOrder(request: CreateOrderRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/create`, request);
  }

  /** 201 Created, but body/Location are unusable (Unit, not the order) — ignore them. */
  addProduct(orderId: string, request: AddProductRequest): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/add/${orderId}`, request);
  }

  increaseQuantity(orderId: string, request: ProductQuantityRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/increase-quantity/${orderId}`, request);
  }

  decreaseQuantity(orderId: string, request: ProductQuantityRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/decrease-quantity/${orderId}`, request);
  }

  removeProduct(orderId: string, request: RemoveProductRequest): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/remove/${orderId}`, request);
  }

  /** Permission is order:create, not order:update — a confirmed backend
   *  inconsistency, not a mistake here (docs/frontend-prd.md §2.3, Submit Order). */
  submitOrder(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/submit/${orderId}`, {});
  }

  cancelOrder(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/cancel/${orderId}`, {});
  }
}
