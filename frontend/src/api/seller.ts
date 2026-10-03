import api from './client'
import type { 
  SellerProfileCreateDto, 
  SellerProfileUpdateDto, 
  SellerProfileResponseDto,
  SellerStatus 
} from '../types/seller'


export const sellerProfileApi = {
  async createSellerProfile(dto: SellerProfileCreateDto): Promise<SellerProfileResponseDto> {
    const response = await api.post<SellerProfileResponseDto>('/SellerProfile', dto)
    return response.data
  },

  async updateSellerProfile(profileId: string, dto: SellerProfileUpdateDto): Promise<string> {
    const response = await api.put<string>(`/SellerProfile/${profileId}`, dto)
    return response.data
  },

  async getCurrentUserSellerProfile(): Promise<SellerProfileResponseDto> {
    const response = await api.get<SellerProfileResponseDto>('/SellerProfile')
    return response.data
  },

  async getUserSellerProfile(userId: string): Promise<SellerProfileResponseDto> {
    const response = await api.get<SellerProfileResponseDto>(`/SellerProfile/${userId}`)
    return response.data
  },

  async getSellersByStatus(status: SellerStatus): Promise<SellerProfileResponseDto[]> {
    const response = await api.get<SellerProfileResponseDto[]>('/SellerProfile/Sellers', {
      params: { status }
    })
    return response.data
  },

  async updateStatusOfSellerProfile(sellerId: string, status: SellerStatus): Promise<string> {
    const response = await api.put<string>(`/SellerProfile/${sellerId}/Status`, null, {
      params: { status }
    })
    return response.data
  },
}