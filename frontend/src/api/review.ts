import api from './client'
import type { 
  CreateReviewDto, 
  UpdateReviewDto, 
  ReviewResponseDto 
} from '../types/review' 


export const reviewApi = {
  async createReview(productId: string, dto: CreateReviewDto): Promise<ReviewResponseDto> {
    const response = await api.post<ReviewResponseDto>(`/Review/${productId}`, dto)
    return response.data
  },

  async updateReview(reviewId: string, dto: UpdateReviewDto): Promise<string> {
    const response = await api.put<string>(`/Review/${reviewId}`, dto)
    return response.data
  },

  async deleteReview(reviewId: string): Promise<string> {
    const response = await api.delete<string>(`/Review/${reviewId}`)
    return response.data
  },

  async getReviewsByProduct(productId: string): Promise<ReviewResponseDto[]> {
    const response = await api.get<ReviewResponseDto[]>(`/Review/${productId}`)
    return response.data
  },

  async getReviewsAdmin(): Promise<ReviewResponseDto[]> {
    const response = await api.get<ReviewResponseDto[]>('/Review/Admin/Reviews')
    return response.data
  },

  async getReviewByIdAdmin(reviewId: string): Promise<ReviewResponseDto> {
    const response = await api.get<ReviewResponseDto>(`/Review/${reviewId}/Admin`)
    return response.data
  },

  async deleteReviewByIdAdmin(reviewId: string): Promise<string> {
    const response = await api.delete<string>(`/Review/${reviewId}/Admin`)
    return response.data
  },
}