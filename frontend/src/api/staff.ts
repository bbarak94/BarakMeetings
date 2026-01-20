import { apiClient } from './client';

export interface StaffDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  serviceIds: string[];
}

export interface WorkingHoursDto {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface TimeSlotDto {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  currentAttendees?: number;
  maxCapacity?: number;
}

export const staffApi = {
  getAll: async (serviceId?: string): Promise<StaffDto[]> => {
    const params = serviceId ? { serviceId } : {};
    const response = await apiClient.get<StaffDto[]>('/staff', { params });
    return response.data;
  },

  getById: async (id: string): Promise<StaffDto> => {
    const response = await apiClient.get<StaffDto>(`/staff/${id}`);
    return response.data;
  },

  getWorkingHours: async (staffId: string): Promise<WorkingHoursDto[]> => {
    const response = await apiClient.get<WorkingHoursDto[]>(`/staff/${staffId}/working-hours`);
    return response.data;
  },

  getAvailability: async (staffId: string, serviceId: string, date: string): Promise<TimeSlotDto[]> => {
    const response = await apiClient.get<TimeSlotDto[]>(`/staff/${staffId}/availability`, {
      params: { serviceId, date },
    });
    return response.data;
  },
};
