export const PrimaryContactType = { Email: 0, Phone: 1 } as const;
export type PrimaryContactType = (typeof PrimaryContactType)[keyof typeof PrimaryContactType];

export interface Address {
  street: string;
  city: string;
  zip: string;
  state: string;
}

/** Shared by POST /api/customers and PUT /api/customers/id — full replace, not a patch. */
export interface CustomerRequest {
  firstName: string;
  middleName: string | null;
  lastName: string;
  address: Address;
  shippingAddress: Address | null;
  email: string | null;
  phone: string | null;
  primaryContactType: PrimaryContactType;
}

/** Shape returned by both GET /api/customers and GET /api/customers/id.
 *  Billing address only — no endpoint ever returns the shipping address. */
export interface Customer {
  id: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  street: string;
  city: string;
  zip: string;
  state: string;
  email: string | null;
  phone: string | null;
}

export interface CreateCustomerResponse {
  customerId: string;
}
