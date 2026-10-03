export interface RegisterRequest
{
    UserName: string;
    Email: string;
    Password: string;
}

export interface LoginRequest
{
    Email: string;
    Password: string;
}

export interface LoginResponse
{
    accessToken: string;
    expiresAt: Date;
}