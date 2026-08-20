import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateCustomerResponse, Customer, CustomerRequest } from './customers.models';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/customers`;

  /** GET /api/customers — bare, unpaginated array. Paged client-side (see PaginationComponent). */
  list(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.baseUrl);
  }

  /**
   * GET /api/customers/id?id=<guid> — the route is literally `/id`, not a
   * `{id}` path parameter (a confirmed backend bug); `id` binds from the
   * query string. See docs/frontend-prd.md §2.2.2.
   */
  getById(id: string): Observable<Customer> {
    return this.http.get<Customer>(`${this.baseUrl}/id`, { params: new HttpParams().set('id', id) });
  }

  create(request: CustomerRequest): Observable<CreateCustomerResponse> {
    return this.http.post<CreateCustomerResponse>(this.baseUrl, request);
  }

  /** PUT /api/customers/id?id=<guid> — same query-bound id quirk as getById. */
  update(id: string, request: CustomerRequest): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/id`, request, { params: new HttpParams().set('id', id) });
  }
}
