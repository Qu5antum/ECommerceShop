export const UserRole = {
  DefaultUser: 1,
  Admin: 2,
  Manager: 4,
  Seller: 8,
  Moderator: 16,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export interface UserResponseDto {
  id: string
  userName: string
  email: string
  roles: UserRole
  isActive: boolean
  createdAt: Date
  updatedAt?: Date | null
}

export interface UserUpdateDto {
  userName?: string | null
  email?: string | null
  password?: string | null
}

export interface UserPreviewResponseDto {
  id: string
  userName: string
  email: string
}
