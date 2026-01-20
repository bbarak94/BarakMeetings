import { apiClient } from './client';

export interface DashboardMetrics {
  totalBookings: number;
  bookingsGrowth: number;
  totalRevenue: number;
  revenueGrowth: number;
  uniqueClients: number;
  clientsGrowth: number;
  completionRate: number;
  cancellationRate: number;
  noShowCount: number;
  averageBookingValue: number;
}

export interface RevenueByService {
  serviceId: string;
  serviceName: string;
  revenue: number;
  bookingCount: number;
}

export interface RevenueByStaff {
  staffId: string;
  staffName: string;
  revenue: number;
  bookingCount: number;
}

export interface BookingTrend {
  date: string;
  count: number;
  revenue: number;
}

export interface PeakHour {
  hour: number;
  bookingCount: number;
  averageRevenue: number;
}

export interface RetentionMetrics {
  newClients: number;
  returningClients: number;
  atRiskClients: number;
  churnedClients: number;
  retentionRate: number;
}

export interface Recommendation {
  type: string;
  title: string;
  description: string;
  priority: number;
}

export const analyticsApi = {
  getDashboard: async (days = 30): Promise<DashboardMetrics> => {
    const response = await apiClient.get<DashboardMetrics>('/analytics/dashboard', {
      params: { days },
    });
    return response.data;
  },

  getRevenueByService: async (days = 30): Promise<RevenueByService[]> => {
    const response = await apiClient.get<RevenueByService[]>('/analytics/revenue/by-service', {
      params: { days },
    });
    return response.data;
  },

  getRevenueByStaff: async (days = 30): Promise<RevenueByStaff[]> => {
    const response = await apiClient.get<RevenueByStaff[]>('/analytics/revenue/by-staff', {
      params: { days },
    });
    return response.data;
  },

  getBookingTrends: async (days = 30): Promise<BookingTrend[]> => {
    const response = await apiClient.get<BookingTrend[]>('/analytics/trends/bookings', {
      params: { days },
    });
    return response.data;
  },

  getPeakHours: async (days = 30): Promise<PeakHour[]> => {
    const response = await apiClient.get<PeakHour[]>('/analytics/insights/peak-hours', {
      params: { days },
    });
    return response.data;
  },

  getRetention: async (days = 30): Promise<RetentionMetrics> => {
    const response = await apiClient.get<RetentionMetrics>('/analytics/insights/retention', {
      params: { days },
    });
    return response.data;
  },

  getRecommendations: async (): Promise<Recommendation[]> => {
    const response = await apiClient.get<Recommendation[]>('/analytics/recommendations');
    return response.data;
  },
};
