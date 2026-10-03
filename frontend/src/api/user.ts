import api from './client';
import type { UserResponseDto, UserUpdateDto } from '../types/user'

export const userApi = {
  async getAllUsersNotAdmin(): Promise<UserResponseDto[]> {
    const response = await api.get<UserResponseDto[]>('/Admin/Users')
    return response.data
  },

  async getUserById(userId: string): Promise<UserResponseDto> {
    const response = await api.get<UserResponseDto>(`/Admin/${userId}`)
    return response.data
  },

  async updateProfile(data: UserUpdateDto): Promise<string> {
    const response = await api.put<string>('/User', data)
    return response.data
  },

  async getCurrentUserProfile(): Promise<UserResponseDto> {
    const response = await api.get<UserResponseDto>('/User/profile')
    return response.data
  },

  async deleteUser(userId: string): Promise<string> {
    const response = await api.delete<string>(`/Admin/Users/${userId}`) 
    return response.data
  },

  async activateOrDeactivateUser(userId: string, isActive: boolean): Promise<string> {
    const response = await api.put<string>(`/Admin/Users/${userId}/ActiveStatus`, null, {
      params: { isActive }
    })
    return response.data
  },

  async addRoleToUser(userId: string, role: number): Promise<string> {
    const response = await api.put<string>(`/Admin/Users/${userId}/RoleAdd`, role)
    return response.data
  },

  async removeRoleFromUser(userId: string, role: number): Promise<string> {
    const response = await api.put<string>(`/Admin/Users/${userId}/RoleRemove`, role)
    return response.data
  },
}