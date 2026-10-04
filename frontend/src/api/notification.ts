import api from './client'
import type { 
  CreateNotificationDto, 
  NotificationResponseDto,
} from '../types/notification'


export const notificationApi = {
  async getNotifications(take?: number, isRead?: boolean): Promise<NotificationResponseDto[]> {
    const params: Record<string, any> = {}
    if (take !== undefined) params.take = take
    if (isRead !== undefined) params.isRead = isRead

    const response = await api.get<NotificationResponseDto[]>('/Notification/Notifications', { params })
    return response.data
  },

  async getNotificationById(notificationId: string): Promise<NotificationResponseDto> {
    const response = await api.get<NotificationResponseDto>(`/Notification/${notificationId}`)
    return response.data
  },

  async getCountOfUnreadNotifications(): Promise<number> {
    const response = await api.get<number>('/Notification/Notifications/count')
    return response.data
  },

  async markNotificationsAsRead(): Promise<string> {
    const response = await api.put<string>('/Notification/MarkAsRead')
    return response.data
  },

  async sendNotificationsAdmin(dto: CreateNotificationDto): Promise<string> {
    const response = await api.post<string>('/Notification/All', dto)
    return response.data
  },

  async getNotificationsAdmin(): Promise<NotificationResponseDto[]> {
    const response = await api.get<NotificationResponseDto[]>('/Notification/Admin/Notifications')
    return response.data
  },

  async getNotificationAdmin(notificationId: string): Promise<NotificationResponseDto> {
    const response = await api.get<NotificationResponseDto>(`/Notification/${notificationId}/Admin`)
    return response.data
  },

  async deleteNotificationAdmin(notificationId: string): Promise<string> {
    const response = await api.delete<string>(`/Notification/${notificationId}/Admin`)
    return response.data
  },
}