import api from './client';
import type { 
  CategoryCreateDto, 
  CategoryUpdateDto, 
  CategoryResponseDto 
} from '../types/category'

export const categoryApi = {
  async createCategory(data: CategoryCreateDto): Promise<CategoryResponseDto> {
    const response = await api.post<CategoryResponseDto>('/Category', data)
    return response.data
  },

  async updateCategory(categoryId: string, data: CategoryUpdateDto): Promise<string> {
    const response = await api.put<string>('/Category', data, {
      params: { categoryId }
    })
    return response.data
  },

  async deleteCategory(categoryId: string): Promise<string> {
    const response = await api.delete<string>('/Category', {
      params: { categoryId }
    })
    return response.data
  },

  async getAllCategories(): Promise<CategoryResponseDto[]> {
    const response = await api.get<CategoryResponseDto[]>('/Category/Categories')
    return response.data
  },

  async getCategoryById(categoryId: string): Promise<CategoryResponseDto> {
    const response = await api.get<CategoryResponseDto>(`/Category/${categoryId}`)
    return response.data
  },
}