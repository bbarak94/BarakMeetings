import { useState, useEffect } from 'react';
import type { ReactElement } from 'react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  Legend,
} from 'recharts';
import { format } from 'date-fns';
import { Card, CardHeader, CardTitle, CardContent } from '../../components/ui';
import { useAuthStore } from '../../stores/authStore';
import { apiClient } from '../../api/client';

interface DashboardMetrics {
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
  periodDays: number;
}

interface ServiceRevenue {
  serviceId: string;
  serviceName: string;
  color: string;
  totalRevenue: number;
  bookingCount: number;
  averagePrice: number;
}

interface DailyTrend {
  date: string;
  bookings: number;
  revenue: number;
  completed: number;
  cancelled: number;
}

interface Recommendation {
  type: string;
  title: string;
  description: string;
  impact: string;
  priority: number;
}

interface RetentionMetrics {
  newClients: number;
  returningClients: number;
  atRiskClients: number;
  totalActiveClients: number;
  retentionRate: number;
  averageBookingsPerClient: number;
  averageLifetimeValue: number;
}

export function AnalyticsDashboard() {
  const { token, user } = useAuthStore();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [serviceRevenue, setServiceRevenue] = useState<ServiceRevenue[]>([]);
  const [trends, setTrends] = useState<DailyTrend[]>([]);
  const [recommendations, setRecommendations] = useState<Recommendation[]>([]);
  const [retention, setRetention] = useState<RetentionMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [period, setPeriod] = useState(30);

  useEffect(() => {
    if (token && user?.tenantId) {
      fetchAnalytics();
    }
  }, [token, user?.tenantId, period]);

  const fetchAnalytics = async () => {
    setLoading(true);
    try {
      const headers = {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': user?.tenantId || '',
      };

      const [dashboardRes, revenueRes, trendsRes, recsRes, retentionRes] = await Promise.all([
        apiClient.get<DashboardMetrics>(`/analytics/dashboard?days=${period}`, { headers }),
        apiClient.get<ServiceRevenue[]>(`/analytics/revenue/by-service?days=${period}`, { headers }),
        apiClient.get<DailyTrend[]>(`/analytics/trends/bookings?days=${period}`, { headers }),
        apiClient.get<Recommendation[]>('/analytics/recommendations', { headers }),
        apiClient.get<RetentionMetrics>(`/analytics/insights/retention?days=${period}`, { headers }),
      ]);

      setMetrics(dashboardRes.data);
      setServiceRevenue(revenueRes.data);
      setTrends(trendsRes.data);
      setRecommendations(recsRes.data);
      setRetention(retentionRes.data);
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  };

  const formatTrendDate = (dateStr: string) => {
    return format(new Date(dateStr), 'MMM d');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Period Selector */}
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Analytics Dashboard</h1>
        <select
          value={period}
          onChange={(e) => setPeriod(Number(e.target.value))}
          className="px-3 py-2 border rounded-lg bg-white dark:bg-gray-800 dark:border-gray-700"
        >
          <option value={7}>Last 7 days</option>
          <option value={30}>Last 30 days</option>
          <option value={90}>Last 90 days</option>
        </select>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title="Total Bookings"
          value={metrics?.totalBookings || 0}
          growth={metrics?.bookingsGrowth || 0}
          icon="calendar"
        />
        <MetricCard
          title="Revenue"
          value={formatCurrency(metrics?.totalRevenue || 0)}
          growth={metrics?.revenueGrowth || 0}
          icon="dollar"
        />
        <MetricCard
          title="Active Clients"
          value={metrics?.uniqueClients || 0}
          growth={metrics?.clientsGrowth || 0}
          icon="users"
        />
        <MetricCard
          title="Completion Rate"
          value={`${metrics?.completionRate || 0}%`}
          subtitle={`${metrics?.cancellationRate || 0}% cancelled`}
          icon="check"
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Booking Trends */}
        <Card>
          <CardHeader>
            <CardTitle>Booking Trends</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={trends}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={formatTrendDate} />
                  <YAxis />
                  <Tooltip
                    labelFormatter={(label) => format(new Date(label), 'MMM d, yyyy')}
                  />
                  <Area
                    type="monotone"
                    dataKey="bookings"
                    stroke="#4F46E5"
                    fill="#4F46E5"
                    fillOpacity={0.2}
                    name="Bookings"
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Revenue by Service */}
        <Card>
          <CardHeader>
            <CardTitle>Revenue by Service</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              {serviceRevenue.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={serviceRevenue as unknown as Record<string, unknown>[]}
                      dataKey="totalRevenue"
                      nameKey="serviceName"
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      label={({ name, percent }: { name?: string; percent?: number }) =>
                        `${name || ''} (${((percent || 0) * 100).toFixed(0)}%)`
                      }
                    >
                      {serviceRevenue.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color || COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => formatCurrency(Number(value))} />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-gray-500">
                  No revenue data yet
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Retention & Performance */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Client Retention */}
        <Card>
          <CardHeader>
            <CardTitle>Client Retention</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex justify-between items-center">
                <span className="text-gray-600 dark:text-gray-400">New Clients</span>
                <span className="font-semibold text-green-600">{retention?.newClients || 0}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600 dark:text-gray-400">Returning Clients</span>
                <span className="font-semibold text-blue-600">{retention?.returningClients || 0}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600 dark:text-gray-400">At Risk (inactive)</span>
                <span className="font-semibold text-orange-600">{retention?.atRiskClients || 0}</span>
              </div>
              <div className="border-t pt-4 mt-4">
                <div className="flex justify-between items-center">
                  <span className="text-gray-600 dark:text-gray-400">Retention Rate</span>
                  <span className="text-xl font-bold text-primary-600">
                    {retention?.retentionRate || 0}%
                  </span>
                </div>
                <div className="flex justify-between items-center mt-2">
                  <span className="text-gray-600 dark:text-gray-400">Avg. Lifetime Value</span>
                  <span className="font-semibold">
                    {formatCurrency(retention?.averageLifetimeValue || 0)}
                  </span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Revenue Breakdown Bar Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Service Performance</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              {serviceRevenue.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={serviceRevenue}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="serviceName" />
                    <YAxis yAxisId="left" orientation="left" stroke="#4F46E5" />
                    <YAxis yAxisId="right" orientation="right" stroke="#10B981" />
                    <Tooltip />
                    <Legend />
                    <Bar
                      yAxisId="left"
                      dataKey="bookingCount"
                      fill="#4F46E5"
                      name="Bookings"
                    />
                    <Bar
                      yAxisId="right"
                      dataKey="totalRevenue"
                      fill="#10B981"
                      name="Revenue"
                    />
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-gray-500">
                  No service data yet
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recommendations */}
      {recommendations.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Recommendations to Improve Profit</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {recommendations.map((rec, index) => (
                <div
                  key={index}
                  className={`p-4 rounded-lg border-l-4 ${
                    rec.type === 'warning'
                      ? 'bg-orange-50 border-orange-500 dark:bg-orange-900/20'
                      : rec.type === 'opportunity'
                      ? 'bg-green-50 border-green-500 dark:bg-green-900/20'
                      : 'bg-blue-50 border-blue-500 dark:bg-blue-900/20'
                  }`}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <h4 className="font-semibold text-gray-900 dark:text-white">{rec.title}</h4>
                      <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                        {rec.description}
                      </p>
                    </div>
                    <span className="text-xs font-medium px-2 py-1 bg-white dark:bg-gray-800 rounded shadow">
                      {rec.impact}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

// Helper Components
const COLORS = ['#4F46E5', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

interface MetricCardProps {
  title: string;
  value: string | number;
  growth?: number;
  subtitle?: string;
  icon: string;
}

function MetricCard({ title, value, growth, subtitle, icon }: MetricCardProps) {
  const iconMap: Record<string, ReactElement> = {
    calendar: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    ),
    dollar: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
    users: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
    ),
    check: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  };

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">{title}</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">{value}</p>
            {growth !== undefined && (
              <p className={`text-sm mt-1 ${growth >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                {growth >= 0 ? '+' : ''}{growth}% vs prev period
              </p>
            )}
            {subtitle && (
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{subtitle}</p>
            )}
          </div>
          <div className="p-3 bg-primary-100 dark:bg-primary-900/20 rounded-full text-primary-600">
            {iconMap[icon]}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
