import api from './client'
import type { 
  PaymentResponseDto, 
  PaymentWebhookDto, 
  PaymentStatus,
  PaymentStatisticsDto 
} from '../types/payment'


export const paymentApi = {
  async createPayment(orderId: string): Promise<PaymentResponseDto> {
    const response = await api.post<PaymentResponseDto>(`/Payment/Order/${orderId}`)
    return response.data
  },

  async getPayment(orderId: string, paymentId: string): Promise<PaymentResponseDto> {
    const response = await api.get<PaymentResponseDto>(`/Payment/${paymentId}/Order/${orderId}`)
    return response.data
  },

  async processWebhook(webhookDto: PaymentWebhookDto): Promise<{ message: string }> {
    const response = await api.put<{ message: string }>('/Payment/Admin/Webhook', webhookDto)
    return response.data
  },

  async getPaymentsAdmin(status?: PaymentStatus): Promise<PaymentResponseDto[]> {
    const params: Record<string, any> = {}
    if (status !== undefined) {
      params.status = status
    }
    const response = await api.get<PaymentResponseDto[]>('/Payment/Payments', { params })
    return response.data
  },

  async getPaymentAdmin(paymentId: string): Promise<PaymentResponseDto> {
    const response = await api.get<PaymentResponseDto>(`/Payment/${paymentId}`)
    return response.data
  },

  async getPaymentsStatistics(): Promise<PaymentStatisticsDto> {
    const response = await api.get<PaymentStatisticsDto>('/Payment/Payments/Statistics')
    return response.data
  },
}