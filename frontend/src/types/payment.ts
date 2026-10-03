export const PaymentStatus = {
  Pending: 1,
  Succeeded: 2,
  Failed: 3,
  Refunded: 4
} as const;

export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export interface PaymentWebhookDto {
  providerPaymentId: string
  status: PaymentStatus
}

export interface PaymentResponseDto {
  id: string
  orderId: string
  userId: string
  amount: number
  status: PaymentStatus
  providerPaymentId?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface PaymentStatisticsDto {
  total: number
  successfull: number
  failed: number
  pending: number
  refunded: number
  revenue: number
}