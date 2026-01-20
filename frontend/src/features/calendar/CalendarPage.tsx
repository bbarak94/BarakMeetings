import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ChevronLeft,
  ChevronRight,
  Loader2,
  Calendar,
  Clock,
  User,
  X,
  Check,
  AlertCircle,
} from 'lucide-react';
import { Button, Card, CardContent, CardHeader, CardTitle } from '../../components/ui';
import { formatCurrency } from '../../lib/utils';
import {
  appointmentsApi,
  type AppointmentDto,
  AppointmentStatus,
  AppointmentStatusLabels,
} from '../../api/appointments';
import {
  format,
  parseISO,
  startOfWeek,
  endOfWeek,
  addDays,
  addWeeks,
  subWeeks,
  isSameDay,
  isToday,
} from 'date-fns';

export function CalendarPage() {
  const queryClient = useQueryClient();
  const [currentDate, setCurrentDate] = useState(new Date());
  const [selectedAppointment, setSelectedAppointment] = useState<AppointmentDto | null>(null);

  const weekStart = startOfWeek(currentDate, { weekStartsOn: 1 }); // Monday
  const weekEnd = endOfWeek(currentDate, { weekStartsOn: 1 });

  const { data: appointments, isLoading } = useQuery({
    queryKey: ['appointments', format(weekStart, 'yyyy-MM-dd'), format(weekEnd, 'yyyy-MM-dd')],
    queryFn: () =>
      appointmentsApi.getAll({
        startDate: format(weekStart, "yyyy-MM-dd'T'00:00:00'Z'"),
        endDate: format(weekEnd, "yyyy-MM-dd'T'23:59:59'Z'"),
      }),
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      appointmentsApi.updateStatus(id, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
      setSelectedAppointment(null);
    },
  });

  const weekDays = useMemo(() => {
    return Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));
  }, [weekStart]);

  const hours = useMemo(() => {
    return Array.from({ length: 12 }, (_, i) => i + 8); // 8 AM to 7 PM
  }, []);

  const getAppointmentsForDay = (day: Date) => {
    if (!appointments) return [];
    return appointments.filter((apt) => {
      const aptDate = parseISO(apt.startTime);
      return isSameDay(aptDate, day);
    });
  };

  const getAppointmentPosition = (apt: AppointmentDto) => {
    const start = parseISO(apt.startTime);
    const hour = start.getHours();
    const minute = start.getMinutes();
    const top = (hour - 8) * 60 + minute; // pixels from 8 AM
    const height = apt.durationMinutes;
    return { top, height };
  };

  const getStatusColor = (status: number) => {
    switch (status) {
      case AppointmentStatus.Pending:
        return 'bg-yellow-100 border-yellow-300 text-yellow-800';
      case AppointmentStatus.Confirmed:
        return 'bg-blue-100 border-blue-300 text-blue-800';
      case AppointmentStatus.InProgress:
        return 'bg-purple-100 border-purple-300 text-purple-800';
      case AppointmentStatus.Completed:
        return 'bg-green-100 border-green-300 text-green-800';
      case AppointmentStatus.Cancelled:
        return 'bg-gray-100 border-gray-300 text-gray-500';
      case AppointmentStatus.NoShow:
        return 'bg-red-100 border-red-300 text-red-800';
      default:
        return 'bg-gray-100 border-gray-300 text-gray-800';
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Calendar</h1>
          <p className="text-gray-500 dark:text-gray-400">
            Manage your appointments
          </p>
        </div>
      </div>

      {/* Week Navigation */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setCurrentDate(subWeeks(currentDate, 1))}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button variant="outline" size="sm" onClick={() => setCurrentDate(new Date())}>
            Today
          </Button>
          <Button variant="outline" size="sm" onClick={() => setCurrentDate(addWeeks(currentDate, 1))}>
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
        <span className="font-medium text-gray-900 dark:text-white">
          {format(weekStart, 'MMM d')} - {format(weekEnd, 'MMM d, yyyy')}
        </span>
      </div>

      {/* Calendar Grid */}
      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex items-center justify-center py-24">
              <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[800px]">
                {/* Day Headers */}
                <div className="grid grid-cols-8 border-b dark:border-gray-700">
                  <div className="p-2 text-center text-sm font-medium text-gray-500 border-r dark:border-gray-700">
                    Time
                  </div>
                  {weekDays.map((day) => (
                    <div
                      key={day.toISOString()}
                      className={`p-2 text-center border-r last:border-r-0 dark:border-gray-700 ${
                        isToday(day) ? 'bg-primary-50 dark:bg-primary-900/20' : ''
                      }`}
                    >
                      <div className="text-xs text-gray-500 uppercase">
                        {format(day, 'EEE')}
                      </div>
                      <div
                        className={`text-lg font-semibold ${
                          isToday(day)
                            ? 'text-primary-600'
                            : 'text-gray-900 dark:text-white'
                        }`}
                      >
                        {format(day, 'd')}
                      </div>
                    </div>
                  ))}
                </div>

                {/* Time Grid */}
                <div className="relative">
                  {hours.map((hour) => (
                    <div key={hour} className="grid grid-cols-8 border-b dark:border-gray-700">
                      <div className="p-2 text-xs text-gray-500 border-r dark:border-gray-700 h-[60px]">
                        {format(new Date().setHours(hour, 0), 'h:mm a')}
                      </div>
                      {weekDays.map((day) => (
                        <div
                          key={`${day.toISOString()}-${hour}`}
                          className={`relative border-r last:border-r-0 dark:border-gray-700 h-[60px] ${
                            isToday(day) ? 'bg-primary-50/50 dark:bg-primary-900/10' : ''
                          }`}
                        >
                          {/* Render appointments that start in this hour */}
                          {getAppointmentsForDay(day)
                            .filter((apt) => {
                              const aptHour = parseISO(apt.startTime).getHours();
                              return aptHour === hour;
                            })
                            .map((apt) => {
                              const { height } = getAppointmentPosition(apt);
                              const minuteOffset = parseISO(apt.startTime).getMinutes();
                              return (
                                <button
                                  key={apt.id}
                                  onClick={() => setSelectedAppointment(apt)}
                                  className={`absolute left-0.5 right-0.5 rounded px-1 text-xs overflow-hidden border ${getStatusColor(
                                    apt.status
                                  )} hover:opacity-80 transition-opacity cursor-pointer z-10`}
                                  style={{
                                    top: `${minuteOffset}px`,
                                    height: `${Math.max(height, 20)}px`,
                                  }}
                                >
                                  <div className="font-medium truncate">{apt.clientName}</div>
                                  {height > 30 && (
                                    <div className="truncate opacity-75">{apt.serviceName}</div>
                                  )}
                                </button>
                              );
                            })}
                        </div>
                      ))}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Appointment Detail Modal */}
      {selectedAppointment && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setSelectedAppointment(null)}
          />
          <Card className="relative z-10 w-full max-w-md mx-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Appointment Details</CardTitle>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedAppointment(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-start gap-4">
                <div
                  className="h-12 w-12 rounded-full flex items-center justify-center flex-shrink-0"
                  style={{ backgroundColor: selectedAppointment.serviceColor || '#6366f1' }}
                >
                  <span className="text-white font-medium">
                    {selectedAppointment.clientName
                      .split(' ')
                      .map((n) => n[0])
                      .join('')}
                  </span>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-900 dark:text-white">
                    {selectedAppointment.clientName}
                  </h3>
                  <p className="text-sm text-gray-500">{selectedAppointment.clientEmail}</p>
                </div>
              </div>

              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-gray-400" />
                  <span>
                    {format(parseISO(selectedAppointment.startTime), 'EEEE, MMMM d, yyyy')}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-gray-400" />
                  <span>
                    {format(parseISO(selectedAppointment.startTime), 'h:mm a')} -{' '}
                    {format(parseISO(selectedAppointment.endTime), 'h:mm a')} (
                    {selectedAppointment.durationMinutes} min)
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <User className="h-4 w-4 text-gray-400" />
                  <span>{selectedAppointment.staffName}</span>
                </div>
              </div>

              <div className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                <div className="flex justify-between items-center">
                  <span className="font-medium">{selectedAppointment.serviceName}</span>
                  <span className="font-semibold text-primary-600">
                    {formatCurrency(selectedAppointment.price)}
                  </span>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Status
                </label>
                <span
                  className={`inline-flex px-3 py-1 rounded-full text-sm ${getStatusColor(
                    selectedAppointment.status
                  )}`}
                >
                  {AppointmentStatusLabels[selectedAppointment.status]}
                </span>
              </div>

              {selectedAppointment.customerNotes && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Customer Notes
                  </label>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    {selectedAppointment.customerNotes}
                  </p>
                </div>
              )}

              {/* Action Buttons */}
              <div className="flex flex-wrap gap-2 pt-4 border-t dark:border-gray-700">
                {selectedAppointment.status === AppointmentStatus.Pending && (
                  <Button
                    size="sm"
                    onClick={() =>
                      updateStatusMutation.mutate({
                        id: selectedAppointment.id,
                        status: AppointmentStatus.Confirmed,
                      })
                    }
                    disabled={updateStatusMutation.isPending}
                  >
                    <Check className="h-4 w-4 mr-1" />
                    Confirm
                  </Button>
                )}
                {selectedAppointment.status === AppointmentStatus.Confirmed && (
                  <Button
                    size="sm"
                    onClick={() =>
                      updateStatusMutation.mutate({
                        id: selectedAppointment.id,
                        status: AppointmentStatus.Completed,
                      })
                    }
                    disabled={updateStatusMutation.isPending}
                  >
                    <Check className="h-4 w-4 mr-1" />
                    Complete
                  </Button>
                )}
                {(selectedAppointment.status === AppointmentStatus.Pending ||
                  selectedAppointment.status === AppointmentStatus.Confirmed) && (
                  <>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() =>
                        updateStatusMutation.mutate({
                          id: selectedAppointment.id,
                          status: AppointmentStatus.NoShow,
                        })
                      }
                      disabled={updateStatusMutation.isPending}
                    >
                      <AlertCircle className="h-4 w-4 mr-1" />
                      No Show
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="text-red-600 border-red-200 hover:bg-red-50"
                      onClick={() =>
                        updateStatusMutation.mutate({
                          id: selectedAppointment.id,
                          status: AppointmentStatus.Cancelled,
                        })
                      }
                      disabled={updateStatusMutation.isPending}
                    >
                      <X className="h-4 w-4 mr-1" />
                      Cancel
                    </Button>
                  </>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
