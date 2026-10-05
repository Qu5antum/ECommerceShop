import api from './client'
import type { 
  SellerProfileCreateDto, 
  SellerProfileUpdateDto, 
  SellerProfileResponseDto,
  SellerStatus,
  SellerPreviewResponseDto
} from '../types/seller'


export const sellerProfileApi = {
  async createSellerProfile(dto: SellerProfileCreateDto): Promise<SellerProfileResponseDto> {
    const formData = new FormData()
    formData.append('StoreName', dto.storeName)
    if (dto.description) {
      formData.append('Description', dto.description)
    }
    if (dto.image) {
      formData.append('Image', dto.image) 
    }

    const response = await api.post<SellerProfileResponseDto>('/SellerProfile', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data
  },

  async updateSellerProfile(profileId: string, dto: SellerProfileUpdateDto): Promise<string> {
    const formData = new FormData()

    if (dto.storeName) {
      formData.append('StoreName', dto.storeName)
    }
    if (dto.description) {
      formData.append('Description', dto.description)
    }
    if (dto.image) {
      formData.append('Image', dto.image) 
    }

    const response = await api.put<string>(`/SellerProfile/${profileId}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data
  },

  async getCurrentUserSellerProfile(): Promise<SellerProfileResponseDto> {
    const response = await api.get<SellerProfileResponseDto>('/SellerProfile')
    return response.data
  },

  async getSellerProfileImage(sellerId: string): Promise<string> {
    const response = await api.get(`/SellerProfile/${sellerId}/Image`, {
      responseType: 'blob',
    })
    return URL.createObjectURL(response.data)
  },

  async getUserSellerProfile(userId: string): Promise<SellerProfileResponseDto> {
    const response = await api.get<SellerProfileResponseDto>(`/SellerProfile/${userId}`)
    return response.data
  },

  async getSellerProfilePreview(sellerId: string): Promise<SellerPreviewResponseDto> {
    const response = await api.get<SellerPreviewResponseDto>(`/SellerProfile/${sellerId}/Preview`)
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