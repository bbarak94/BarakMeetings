import { apiClient } from './client';

export interface ClientDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  notes?: string;
  allowMarketing: boolean;
  isActive: boolean;
  appointmentCount: number;
  lastVisitDate?: string;
  totalSpent: number;
}

export interface CreateClientRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  notes?: string;
  allowMarketing?: boolean;
}

export interface UpdateClientRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  phoneNumber?: string;
  notes?: string;
  allowMarketing?: boolean;
  isActive?: boolean;
}

export interface ClientAppointmentDto {
  id: string;
  serviceName: string;
  staffName: string;
  startTime: string;
  endTime: string;
  price: number;
  status: number;
}

export const clientsApi = {
  getAll: async (search?: string, activeOnly = true): Promise<ClientDto[]> => {
    const response = await apiClient.get<ClientDto[]>('/clients', {
      params: { search, activeOnly },
    });
    return response.data;
  },

  getById: async (id: string): Promise<ClientDto> => {
    const response = await apiClient.get<ClientDto>(`/clients/${id}`);
    return response.data;
  },

  create: async (data: CreateClientRequest): Promise<ClientDto> => {
    const response = await apiClient.post<ClientDto>('/clients', data);
    return response.data;
  },

  update: async (id: string, data: UpdateClientRequest): Promise<void> => {
    await apiClient.put(`/clients/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/clients/${id}`);
  },

  getAppointments: async (clientId: string): Promise<ClientAppointmentDto[]> => {
    const response = await apiClient.get<ClientAppointmentDto[]>(`/clients/${clientId}/appointments`);
    return response.data;
  },
};
