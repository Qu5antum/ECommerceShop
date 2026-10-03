import api from './client';
import type { 
  CartItemCreateDto, 
  CartItemUpdateDto, 
  CartItemResponseDto, 
  CartResponseDto 
} from '../types/cart' 


export const cartApi = {
  async getCart(): Promise<CartResponseDto> {
    const response = await api.get<CartResponseDto>('/Cart')
    return response.data
  },
}

export const cartItemApi = {
  async createCartItem(itemCreateDto: CartItemCreateDto): Promise<CartItemResponseDto> {
    const response = await api.post<CartItemResponseDto>('/CartItem', itemCreateDto)
    return response.data
  },

  async updateCartItem(itemId: string, itemUpdateDto: CartItemUpdateDto): Promise<string> {
    const response = await api.put<string>(`/CartItem/${itemId}`, itemUpdateDto)
    return response.data
  },

  async deleteCartItem(itemId: string): Promise<string> {
    const response = await api.delete<string>(`/CartItem/${itemId}`)
    return response.data
  },

  async getAllItemsFromCart(): Promise<CartItemResponseDto[]> {
    const response = await api.get<CartItemResponseDto[]>('/CartItem/CartItems')
    return response.data
  },

  async getItemInCartById(itemId: string): Promise<CartItemResponseDto> {
    const response = await api.get<CartItemResponseDto>(`/CartItem/${itemId}`)
    return response.data
  },
}