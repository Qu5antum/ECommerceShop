import type { UserReviewPreviewResponseDto } from "./user"

export interface CreateReviewDto {
  rating: number
  comment: string
}

export interface UpdateReviewDto {
  rating?: number | null
  comment?: string | null
}

export interface ProductReviewsResponseDto {
  id: string
  user?: UserReviewPreviewResponseDto | null
  rating: number
  comment: string
  createdAt: string
  updatedAt?: string | null
}

export interface ReviewResponseDto {
  id: string
  userId: string
  productId: string
  rating: number
  comment: string
  createdAt: Date
  updatedAt?: Date | null 
}