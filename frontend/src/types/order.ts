export const  OrderStatus = {
  Pending: 1,
  PaymentPending: 2,
  Paid: 3,
  Processing: 4,
  Shipped: 5,
  Delivered: 6,
  Cancelled: 7
} as const;

export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

export interface OrderItemResponseDto {
  id: string
  orderId: string
  productId: string
  productName: string
  price: number
  quantity: number
  createdAt: string
  updatedAt?: string | null
}

export interface OrderResponseWithOutItemsDto {
  id: string
  userId: string
  totalAmount: number
  status: OrderStatus
  createdAt: string
  updatedAt?: string | null
}

export interface OrderResponseDto {
  id: string
  userId: string
  totalAmount: number
  status: OrderStatus
  orderItems: OrderItemResponseDto[]
  createdAt: string
  updatedAt?: string | null
}

export interface UpdateOrderStatusDto {
  status: OrderStatus
}