export const SellerStatus =  {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
  Suspended: 3,
} as const;

export type SellerStatus = (typeof SellerStatus)[keyof typeof SellerStatus];

export interface SellerProfileCreateDto {
  storeName: string
  description?: string | null
  image?: File | null
}

export interface SellerProfileUpdateDto {
  storeName?: string | null
  description?: string | null
  image?: File | null
}

export interface SellerProfileResponseDto {
  id: string
  userId: string
  storeName: string
  description?: string | null
  imageUrl?: string | null
  status: SellerStatus
  createdAt: Date
  updatedAt?: Date | null
}

export interface SellerPreviewResponseDto {
  storeName: string
  description?: string | null
}