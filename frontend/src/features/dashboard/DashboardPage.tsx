import { useQuery } from '@tanstack/react-query';
import { Calendar, Users, DollarSign, Clock, TrendingUp, TrendingDown, Loader2 } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '../../components/ui';
import { formatCurrency } from '../../lib/utils';
import { analyticsApi } from '../../api/analytics';
import { appointmentsApi, AppointmentStatusLabels } from '../../api/appointments';
import { clientsApi } from '../../api/clients';
import { format, parseISO } from 'date-fns';

export function DashboardPage() {
  const { data: metrics, isLoading: metricsLoading } = useQuery({
    queryKey: ['dashboard-metrics'],
    queryFn: () => analyticsApi.getDashboard(30),
  });

  const { data: upcomingAppointments, isLoading: appointmentsLoading } = useQuery({
    queryKey: ['upcoming-appointments'],
    queryFn: () => appointmentsApi.getUpcoming(5),
  });

  const { data: clients } = useQuery({
    queryKey: ['clients-count'],
    queryFn: () => clientsApi.getAll(),
  });

  const stats = [
    {
      name: "Today's Appointments",
      value: metrics?.totalBookings?.toString() ?? '-',
      icon: Calendar,
      change: metrics?.bookingsGrowth
        ? `${metrics.bookingsGrowth > 0 ? '+' : ''}${metrics.bookingsGrowth.toFixed(1)}% from last period`
        : 'Loading...',
      trend: metrics?.bookingsGrowth ?? 0,
    },
    {
      name: 'Total Clients',
      value: clients?.length?.toString() ?? '-',
      icon: Users,
      change: metrics?.clientsGrowth
        ? `${metrics.clientsGrowth > 0 ? '+' : ''}${metrics.clientsGrowth.toFixed(1)}% growth`
        : 'Loading...',
      trend: metrics?.clientsGrowth ?? 0,
    },
    {
      name: 'Revenue (30 days)',
      value: metrics?.totalRevenue !== undefined ? formatCurrency(metrics.totalRevenue) : '-',
      icon: DollarSign,
      change: metrics?.revenueGrowth
        ? `${metrics.revenueGrowth > 0 ? '+' : ''}${metrics.revenueGrowth.toFixed(1)}% from last period`
        : 'Loading...',
      trend: metrics?.revenueGrowth ?? 0,
    },
    {
      name: 'Completion Rate',
      value: metrics?.completionRate !== undefined ? `${metrics.completionRate.toFixed(0)}%` : '-',
      icon: Clock,
      change: `${metrics?.noShowCount ?? 0} no-shows`,
      trend: metrics?.completionRate ?? 0 > 80 ? 1 : -1,
    },
  ];

  const isLoading = metricsLoading || appointmentsLoading;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Dashboard</h1>
        <p className="text-gray-500 dark:text-gray-400">
          Welcome back! Here's what's happening today.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((stat) => (
          <Card key={stat.name}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500 dark:text-gray-400">
                    {stat.name}
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">
                    {isLoading ? (
                      <Loader2 className="h-6 w-6 animate-spin" />
                    ) : (
                      stat.value
                    )}
                  </p>
                  <div className="flex items-center gap-1 mt-1">
                    {stat.trend > 0 ? (
                      <TrendingUp className="h-3 w-3 text-green-500" />
                    ) : stat.trend < 0 ? (
                      <TrendingDown className="h-3 w-3 text-red-500" />
                    ) : null}
                    <p className={`text-xs ${stat.trend > 0 ? 'text-green-600' : stat.trend < 0 ? 'text-red-600' : 'text-gray-500'} dark:text-gray-400`}>
                      {stat.change}
                    </p>
                  </div>
                </div>
                <div className="h-12 w-12 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center">
                  <stat.icon className="h-6 w-6 text-primary-600 dark:text-primary-400" />
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Upcoming Appointments */}
      <Card>
        <CardHeader>
          <CardTitle>Upcoming Appointments</CardTitle>
        </CardHeader>
        <CardContent>
          {appointmentsLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
            </div>
          ) : upcomingAppointments && upcomingAppointments.length > 0 ? (
            <div className="divide-y dark:divide-gray-700">
              {upcomingAppointments.map((appointment) => (
                <div key={appointment.id} className="py-4 flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div
                      className="h-10 w-10 rounded-full flex items-center justify-center"
                      style={{ backgroundColor: appointment.serviceColor || '#6366f1' }}
                    >
                      <span className="text-sm font-medium text-white">
                        {appointment.clientName
                          .split(' ')
                          .map((n) => n[0])
                          .join('')}
                      </span>
                    </div>
                    <div>
                      <p className="font-medium text-gray-900 dark:text-white">
                        {appointment.clientName}
                      </p>
                      <p className="text-sm text-gray-500 dark:text-gray-400">
                        {appointment.serviceName} with {appointment.staffName}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-gray-900 dark:text-white">
                      {format(parseISO(appointment.startTime), 'h:mm a')}
                    </p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                      {format(parseISO(appointment.startTime), 'MMM d')}
                    </p>
                    <span
                      className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                        appointment.status === 1
                          ? 'bg-green-100 text-green-800'
                          : appointment.status === 0
                          ? 'bg-yellow-100 text-yellow-800'
                          : 'bg-gray-100 text-gray-800'
                      }`}
                    >
                      {AppointmentStatusLabels[appointment.status]}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400">
              <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No upcoming appointments</p>
              <p className="text-sm">Appointments will appear here once scheduled</p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
