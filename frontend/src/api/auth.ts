import api from './client';
import type { RegisterRequest, LoginRequest, LoginResponse } from "../types/auth"; 


export const authApi = {
    async register(data: RegisterRequest): Promise<{ message: string }> {
        const response = await api.post<{ message: string }>('/auth/register', data);
        return response.data;
    },

    async createAdmin(data: RegisterRequest): Promise<{ message: string }> {
        const response = await api.post<{ message: string }>('/auth/adminregister', data);
        return response.data;
    },

    async login(data: LoginRequest): Promise<LoginResponse> {
        const response = await api.post<LoginResponse>('/auth/login', data);
        return response.data;
    }
};