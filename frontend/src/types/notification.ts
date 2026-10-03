export const NotificationType = {
    General: 0,
    PaymentSuccess: 1,
    PaymentFailed: 2,
    OrderShipped: 3,
    OrderDelivered: 4,
    OrderCancelled: 5
} as const;

export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export interface CreateNotificationDto {
  title: string
  message: string
}

export interface NotificationResponseDto {
  id: string
  userId: string
  title: string
  message: string
  isRead: boolean
  type: NotificationType
  createdAt: Date
  updatedAt?: Date | null
}