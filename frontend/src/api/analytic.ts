import api from './client';
import type { GeneralAnalyticsResponseDto } from '../types/analytic'

export const analyticApi = {
  async getGeneralAnalyticsAdmin(): Promise<GeneralAnalyticsResponseDto> {
    const response = await api.get<GeneralAnalyticsResponseDto>('/GeneralAnatytic/Admin/Dashboard/Summary')
    return response.data
  },
}