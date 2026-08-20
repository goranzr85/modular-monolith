/** POST /api/products and PUT /api/products share this exact shape. */
export interface ProductRequest {
  sku: string;
  name: string;
  description: string;
  price: number;
}
