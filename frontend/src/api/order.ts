import api from './client'
import type { 
  OrderResponseDto, 
  UpdateOrderStatusDto, 
  OrderStatus 
} from '../types/order'


export const orderApi = {
  async createOrder(): Promise<OrderResponseDto> {
    const response = await api.post<OrderResponseDto>('/Order')
    return response.data
  },

  async getOrdersOfUser(): Promise<OrderResponseDto[]> {
    const response = await api.get<OrderResponseDto[]>('/Order/Orders')
    return response.data
  },

  async getOrderOfUser(orderId: string): Promise<OrderResponseDto> {
    const response = await api.get<OrderResponseDto>(`/Order/${orderId}`)
    return response.data
  },

  async cancelOrder(orderId: string): Promise<string> {
    const response = await api.delete<string>(`/Order/${orderId}/Cancel`)
    return response.data
  },

  async getOrdersOfSeller(): Promise<OrderResponseDto[]> {
    const response = await api.get<OrderResponseDto[]>('/Order/Admin/Orders')
    return response.data
  },

  async updateStatusOfOrder(orderId: string, dto: UpdateOrderStatusDto): Promise<string> {
    const response = await api.put<string>(`/Order/${orderId}/Status`, dto)
    return response.data
  },

  async getOrdersAdmin(status?: OrderStatus): Promise<OrderResponseDto[]> {
    const params: Record<string, any> = {}
    if (status !== undefined) {
      params.status = status
    }
    const response = await api.get<OrderResponseDto[]>('/Order/Orders/Admin', { params })
    return response.data
  },

  async getOrderAdmin(orderId: string): Promise<OrderResponseDto> {
    const response = await api.get<OrderResponseDto>(`/Order/${orderId}/Admin`)
    return response.data
  },

  async getOrdersByUserIdAdmin(userId: string): Promise<OrderResponseDto[]> {
    const response = await api.get<OrderResponseDto[]>(`/Order/User/${userId}/Admin`)
    return response.data
  },

  async getOrdersFromDateToDateAdmin(fromDate: string, toDate: string): Promise<OrderResponseDto[]> {
    const response = await api.get<OrderResponseDto[]>('/Order/Admin/Date', {
      params: { fromDate, toDate }
    })
    return response.data
  },
}