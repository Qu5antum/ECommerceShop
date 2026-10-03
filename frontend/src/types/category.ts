export interface CategoryCreateDto {
  title: string
  slug: string
}

export interface CategoryUpdateDto {
  title?: string | null
  slug?: string | null
}

export interface CategoryResponseDto {
  id: string
  title: string
  slug: string
  createdAt: string
  updatedAt?: Date | null
}