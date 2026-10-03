export interface CartItemCreateDto {
  productId: string
  quantity: number
}

export interface CartItemUpdateDto {
  quantity: number
}

export interface CartItemResponseDto {
  id: string
  cartId: string
  productId: string
  quantity: number
  createdAt: Date 
  updatedAt?: Date | null 
}

export interface CartResponseDto {
  id: string
  userId: string
  items?: CartItemResponseDto[]
}