export interface ProductResponseDto {
  id: string
  sellerProfileId: string
  categoryId: string
  name: string
  description: string
  price: number
  sku: string
  stock: number
  imageUrl?: string | null
  createdAt: Date
  updatedAt?: Date | null
}

export interface ProductCreateData {
  categoryId: string
  name: string
  description: string
  price: number
  image?: File | null
  sku: string
  stock: number
}

export interface ProductUpdateData {
  categoryId?: string | null
  name?: string | null
  description?: string | null
  price?: number | null
  image?: File | null
  sku?: string | null
  stock?: number | null
}