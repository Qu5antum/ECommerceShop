import api from './client';
import type { UserResponseDto, UserUpdateDto, UserPasswordUpdateDto } from '../types/user'

export const userApi = {
  async getAllUsersNotAdmin(): Promise<UserResponseDto[]> {
    const response = await api.get<UserResponseDto[]>('/User/Admin/Users')
    return response.data
  },

  async getUserById(userId: string): Promise<UserResponseDto> {
    const response = await api.get<UserResponseDto>(`/User/Admin/${userId}`)
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

  async changePassword(data: UserPasswordUpdateDto): Promise<string> {
    const response = await api.put<string>('/User/ChangePassword', data)
    return response.data
  },

  async deleteUser(userId: string): Promise<string> {
    const response = await api.delete<string>(`/User/Admin/${userId}`) 
    return response.data
  },

  async activateOrDeactivateUser(userId: string, isActive: boolean): Promise<string> {
    const response = await api.put<string>(`/User/Admin/${userId}/ActiveStatus`, null, {
      params: { isActive }
    })
    return response.data 
  },

  async addRoleToUser(userId: string, role: number): Promise<string> {
    const response = await api.put<string>(`/User/Admin/${userId}/RoleAdd`, null, {
      params: { role }
    })
    return response.data
  },

  async removeRoleFromUser(userId: string, role: number): Promise<string> {
    const response = await api.put<string>(`/User/Admin/${userId}/RoleRemove`, null, {
      params: { role }
    })
    return response.data
  },
}