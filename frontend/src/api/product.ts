import api from './client';
import type { 
  ProductResponseDto, 
  ProductCreateData, 
  ProductUpdateData 
} from '../types/product'

export const productApi = {
  async createProduct(data: ProductCreateData): Promise<ProductResponseDto> {
    const formData = new FormData()
    formData.append('categoryId', data.categoryId)
    formData.append('Name', data.name)
    formData.append('Description', data.description)
    formData.append('Price', data.price.toString())
    formData.append('SKU', data.sku)
    formData.append('Stock', data.stock.toString())
    
    if (data.image) {
      formData.append('Image', data.image)
    }

    const response = await api.post<ProductResponseDto>('/Product', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data
  },

  async updateProduct(productId: string, data: ProductUpdateData): Promise<string> {
    const formData = new FormData()
    if (data.categoryId) formData.append('categoryId', data.categoryId)
    if (data.name) formData.append('Name', data.name)
    if (data.description) formData.append('Description', data.description)
    if (data.price !== undefined && data.price !== null) formData.append('Price', data.price.toString())
    if (data.sku) formData.append('SKU', data.sku)
    if (data.stock !== undefined && data.stock !== null) formData.append('Stock', data.stock.toString())
    
    if (data.image) {
      formData.append('Image', data.image)
    }

    const response = await api.put<string>(`/Product/${productId}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data
  },

  async deleteProduct(productId: string): Promise<string> {
    const response = await api.delete<string>(`/Product/${productId}`)
    return response.data
  },

  async getProductById(productId: string): Promise<ProductResponseDto> {
    const response = await api.get<ProductResponseDto>(`/Product/${productId}`)
    return response.data
  },

  async getProducts(): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>('/Product/Products')
    return response.data
  },

  async getProductImage(productId: string): Promise<string> {
    const response = await api.get(`/Product/${productId}/image`, {
      responseType: 'blob',
    })
    return URL.createObjectURL(response.data)
  },

  async getProductsByCategoryId(categoryId: string): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>(`/Product/Category/${categoryId}`)
    return response.data
  },

  async searchProduct(productName: string): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>('/Product/Search', {
      params: { productName }
    })
    return response.data
  },

  async searchProductAsc(productName: string): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>('/Product/Search/Min', {
      params: { productName }
    })
    return response.data
  },

  async searchProductDesc(productName: string): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>('/Product/Search/Max', {
      params: { productName }
    })
    return response.data
  },

  async deleteProductAdmin(productId: string): Promise<string> {
    const response = await api.delete<string>(`/Product/Admin/${productId}`)
    return response.data
  },

  async getProductsOutOfStock(): Promise<ProductResponseDto[]> {
    const response = await api.get<ProductResponseDto[]>('/Product/Admin')
    return response.data
  },
}