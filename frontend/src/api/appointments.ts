import { apiClient } from './client';

export interface AppointmentDto {
  id: string;
  serviceId: string;
  serviceName: string;
  serviceColor?: string;
  staffId: string;
  staffName: string;
  clientId: string;
  clientName: string;
  clientEmail: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  price: number;
  status: number;
  customerNotes?: string;
  internalNotes?: string;
}

export interface CreateAppointmentRequest {
  serviceId: string;
  staffId: string;
  startTime: string;
  clientId?: string;
  clientEmail?: string;
  clientFirstName?: string;
  clientLastName?: string;
  clientPhone?: string;
  notes?: string;
}

export interface UpdateStatusRequest {
  status: number;
  reason?: string;
}

export interface UpdateAppointmentRequest {
  startTime?: string;
  staffId?: string;
  internalNotes?: string;
}

export interface AppointmentFilters {
  startDate?: string;
  endDate?: string;
  staffId?: string;
  clientId?: string;
  status?: number;
}

export const AppointmentStatus = {
  Pending: 0,
  Confirmed: 1,
  InProgress: 2,
  Completed: 3,
  Cancelled: 4,
  NoShow: 5,
} as const;

export const AppointmentStatusLabels: Record<number, string> = {
  0: 'Pending',
  1: 'Confirmed',
  2: 'In Progress',
  3: 'Completed',
  4: 'Cancelled',
  5: 'No Show',
};

export const appointmentsApi = {
  getAll: async (filters?: AppointmentFilters): Promise<AppointmentDto[]> => {
    const response = await apiClient.get<AppointmentDto[]>('/appointments', { params: filters });
    return response.data;
  },

  getById: async (id: string): Promise<AppointmentDto> => {
    const response = await apiClient.get<AppointmentDto>(`/appointments/${id}`);
    return response.data;
  },

  getUpcoming: async (limit = 10): Promise<AppointmentDto[]> => {
    const response = await apiClient.get<AppointmentDto[]>('/appointments/upcoming', {
      params: { limit },
    });
    return response.data;
  },

  create: async (data: CreateAppointmentRequest): Promise<AppointmentDto> => {
    const response = await apiClient.post<AppointmentDto>('/appointments', data);
    return response.data;
  },

  updateStatus: async (id: string, data: UpdateStatusRequest): Promise<void> => {
    await apiClient.put(`/appointments/${id}/status`, data);
  },

  update: async (id: string, data: UpdateAppointmentRequest): Promise<void> => {
    await apiClient.put(`/appointments/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/appointments/${id}`);
  },
};
