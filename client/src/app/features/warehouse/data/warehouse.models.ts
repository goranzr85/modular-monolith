export interface ReceiveStockRequest {
  quantity: number;
}

export interface ShipStockRequest {
  orderId: string;
  quantity: number;
}

export interface AdjustStockRequest {
  quantity: number;
  reason: string;
}
