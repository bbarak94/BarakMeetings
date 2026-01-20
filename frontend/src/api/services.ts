import { apiClient } from './client';

// Types matching backend DTOs
export interface ServiceDto {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  capacity: number;
  isGroupClass: boolean;
  color?: string;
}

export interface CreateServiceRequest {
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  capacity?: number;
  bufferMinutes?: number;
  color?: string;
  sortOrder?: number;
}

export interface UpdateServiceRequest {
  name?: string;
  description?: string;
  durationMinutes?: number;
  price?: number;
  capacity?: number;
  bufferMinutes?: number;
  color?: string;
  isActive?: boolean;
}

export const servicesApi = {
  getAll: async (): Promise<ServiceDto[]> => {
    const response = await apiClient.get<ServiceDto[]>('/services');
    return response.data;
  },

  getById: async (id: string): Promise<ServiceDto> => {
    const response = await apiClient.get<ServiceDto>(`/services/${id}`);
    return response.data;
  },

  create: async (data: CreateServiceRequest): Promise<ServiceDto> => {
    const response = await apiClient.post<ServiceDto>('/services', data);
    return response.data;
  },

  update: async (id: string, data: UpdateServiceRequest): Promise<void> => {
    await apiClient.put(`/services/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/services/${id}`);
  },
};
