import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Check, Calendar, User, ChevronRight, Loader2, AlertCircle } from 'lucide-react';
import { Button, Card, CardHeader, CardTitle, CardContent, Input } from '../../components/ui';
import { cn, formatCurrency, formatDuration } from '../../lib/utils';
import { servicesApi } from '../../api/services';
import { staffApi, type TimeSlotDto } from '../../api/staff';
import { appointmentsApi } from '../../api/appointments';
import { format, parseISO } from 'date-fns';

type Step = 'service' | 'staff' | 'datetime' | 'details' | 'confirm';

const steps: { id: Step; name: string }[] = [
  { id: 'service', name: 'Service' },
  { id: 'staff', name: 'Staff' },
  { id: 'datetime', name: 'Date & Time' },
  { id: 'details', name: 'Your Details' },
  { id: 'confirm', name: 'Confirm' },
];

export function BookingWizard() {
  const { tenantSlug } = useParams();
  const [currentStep, setCurrentStep] = useState<Step>('service');
  const [selectedService, setSelectedService] = useState<string | null>(null);
  const [selectedStaff, setSelectedStaff] = useState<string | null>(null);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlotDto | null>(null);
  const [bookingSuccess, setBookingSuccess] = useState(false);

  // Client details
  const [clientDetails, setClientDetails] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    notes: '',
  });

  // Fetch services
  const { data: services, isLoading: servicesLoading } = useQuery({
    queryKey: ['booking-services'],
    queryFn: () => servicesApi.getAll(),
  });

  // Fetch staff for selected service
  const { data: staff, isLoading: staffLoading } = useQuery({
    queryKey: ['booking-staff', selectedService],
    queryFn: () => staffApi.getAll(selectedService || undefined),
    enabled: !!selectedService,
  });

  // Fetch availability
  const { data: availability, isLoading: availabilityLoading } = useQuery({
    queryKey: ['booking-availability', selectedStaff, selectedService, selectedDate],
    queryFn: () => {
      if (!selectedStaff || selectedStaff === 'any' || !selectedService || !selectedDate) {
        return Promise.resolve([]);
      }
      return staffApi.getAvailability(selectedStaff, selectedService, selectedDate);
    },
    enabled: !!selectedStaff && selectedStaff !== 'any' && !!selectedService && !!selectedDate,
  });

  // Book appointment mutation
  const bookMutation = useMutation({
    mutationFn: appointmentsApi.create,
    onSuccess: () => {
      setBookingSuccess(true);
    },
  });

  const currentStepIndex = steps.findIndex((s) => s.id === currentStep);

  const goToNext = () => {
    const nextIndex = currentStepIndex + 1;
    if (nextIndex < steps.length) {
      setCurrentStep(steps[nextIndex].id);
    }
  };

  const goToPrev = () => {
    const prevIndex = currentStepIndex - 1;
    if (prevIndex >= 0) {
      setCurrentStep(steps[prevIndex].id);
    }
  };

  const handleBooking = () => {
    if (!selectedService || !selectedStaff || !selectedSlot) return;

    bookMutation.mutate({
      serviceId: selectedService,
      staffId: selectedStaff === 'any' ? staff?.[0]?.id || '' : selectedStaff,
      startTime: selectedSlot.startTime,
      clientEmail: clientDetails.email,
      clientFirstName: clientDetails.firstName,
      clientLastName: clientDetails.lastName,
      clientPhone: clientDetails.phone || undefined,
      notes: clientDetails.notes || undefined,
    });
  };

  const selectedServiceData = services?.find((s) => s.id === selectedService);
  const selectedStaffData = staff?.find((s) => s.id === selectedStaff);

  const canContinue = () => {
    switch (currentStep) {
      case 'service':
        return !!selectedService;
      case 'staff':
        return !!selectedStaff;
      case 'datetime':
        return !!selectedDate && !!selectedSlot;
      case 'details':
        return clientDetails.firstName && clientDetails.lastName && clientDetails.email;
      default:
        return true;
    }
  };

  if (bookingSuccess) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4">
        <div className="max-w-md mx-auto text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Check className="h-8 w-8 text-green-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
            Booking Confirmed!
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mb-6">
            Your appointment has been scheduled. You'll receive a confirmation email shortly.
          </p>
          <Card>
            <CardContent className="p-6 text-left">
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-500">Service</span>
                  <span className="font-medium">{selectedServiceData?.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Staff</span>
                  <span className="font-medium">
                    {selectedStaff === 'any' ? 'Any Available' : selectedStaffData?.fullName}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Date & Time</span>
                  <span className="font-medium">
                    {selectedSlot && format(parseISO(selectedSlot.startTime), 'MMM d, yyyy h:mm a')}
                  </span>
                </div>
                <div className="border-t pt-3 flex justify-between">
                  <span className="font-medium">Total</span>
                  <span className="font-bold text-primary-600">
                    {selectedServiceData && formatCurrency(selectedServiceData.price)}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Book an Appointment
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1 capitalize">
            {tenantSlug?.replace(/-/g, ' ')}
          </p>
        </div>

        {/* Progress Steps */}
        <div className="flex items-center justify-center mb-8 overflow-x-auto">
          {steps.map((step, index) => (
            <div key={step.id} className="flex items-center">
              <div
                className={cn(
                  'flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium',
                  index < currentStepIndex
                    ? 'bg-primary-600 text-white'
                    : index === currentStepIndex
                    ? 'bg-primary-600 text-white'
                    : 'bg-gray-200 text-gray-500 dark:bg-gray-700 dark:text-gray-400'
                )}
              >
                {index < currentStepIndex ? (
                  <Check className="h-4 w-4" />
                ) : (
                  index + 1
                )}
              </div>
              {index < steps.length - 1 && (
                <div
                  className={cn(
                    'w-8 md:w-12 h-0.5 mx-1 md:mx-2',
                    index < currentStepIndex
                      ? 'bg-primary-600'
                      : 'bg-gray-200 dark:bg-gray-700'
                  )}
                />
              )}
            </div>
          ))}
        </div>

        {/* Step Content */}
        <Card>
          <CardHeader>
            <CardTitle>
              {currentStep === 'service' && 'Select a Service'}
              {currentStep === 'staff' && 'Choose Your Provider'}
              {currentStep === 'datetime' && 'Pick a Date & Time'}
              {currentStep === 'details' && 'Your Details'}
              {currentStep === 'confirm' && 'Confirm Your Booking'}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {/* Service Selection */}
            {currentStep === 'service' && (
              <div className="space-y-3">
                {servicesLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
                  </div>
                ) : services && services.length > 0 ? (
                  services.map((service) => (
                    <button
                      key={service.id}
                      onClick={() => setSelectedService(service.id)}
                      className={cn(
                        'w-full p-4 rounded-lg border-2 text-left transition-colors',
                        selectedService === service.id
                          ? 'border-primary-600 bg-primary-50 dark:bg-primary-900/20'
                          : 'border-gray-200 hover:border-gray-300 dark:border-gray-700 dark:hover:border-gray-600'
                      )}
                    >
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="font-medium text-gray-900 dark:text-white">
                            {service.name}
                          </p>
                          <p className="text-sm text-gray-500 dark:text-gray-400">
                            {formatDuration(service.durationMinutes)}
                            {service.isGroupClass && (
                              <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs bg-blue-100 text-blue-800">
                                Group Class
                              </span>
                            )}
                          </p>
                          {service.description && (
                            <p className="text-sm text-gray-400 mt-1">{service.description}</p>
                          )}
                        </div>
                        <p className="font-medium text-gray-900 dark:text-white">
                          {formatCurrency(service.price)}
                        </p>
                      </div>
                    </button>
                  ))
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    No services available
                  </div>
                )}
              </div>
            )}

            {/* Staff Selection */}
            {currentStep === 'staff' && (
              <div className="space-y-3">
                {staffLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
                  </div>
                ) : (
                  <>
                    <button
                      onClick={() => setSelectedStaff('any')}
                      className={cn(
                        'w-full p-4 rounded-lg border-2 text-left transition-colors',
                        selectedStaff === 'any'
                          ? 'border-primary-600 bg-primary-50 dark:bg-primary-900/20'
                          : 'border-gray-200 hover:border-gray-300 dark:border-gray-700 dark:hover:border-gray-600'
                      )}
                    >
                      <p className="font-medium text-gray-900 dark:text-white">
                        No Preference
                      </p>
                      <p className="text-sm text-gray-500 dark:text-gray-400">
                        First available staff member
                      </p>
                    </button>
                    {staff?.map((member) => (
                      <button
                        key={member.id}
                        onClick={() => setSelectedStaff(member.id)}
                        className={cn(
                          'w-full p-4 rounded-lg border-2 text-left transition-colors',
                          selectedStaff === member.id
                            ? 'border-primary-600 bg-primary-50 dark:bg-primary-900/20'
                            : 'border-gray-200 hover:border-gray-300 dark:border-gray-700 dark:hover:border-gray-600'
                        )}
                      >
                        <div className="flex items-center gap-4">
                          <div className="h-12 w-12 rounded-full bg-primary-100 flex items-center justify-center">
                            {member.avatarUrl ? (
                              <img
                                src={member.avatarUrl}
                                alt={member.fullName}
                                className="h-12 w-12 rounded-full object-cover"
                              />
                            ) : (
                              <User className="h-6 w-6 text-primary-600" />
                            )}
                          </div>
                          <div>
                            <p className="font-medium text-gray-900 dark:text-white">
                              {member.fullName}
                            </p>
                            {member.title && (
                              <p className="text-sm text-gray-500 dark:text-gray-400">
                                {member.title}
                              </p>
                            )}
                          </div>
                        </div>
                      </button>
                    ))}
                  </>
                )}
              </div>
            )}

            {/* Date & Time Selection */}
            {currentStep === 'datetime' && (
              <div className="space-y-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Select Date
                  </label>
                  <input
                    type="date"
                    value={selectedDate || ''}
                    onChange={(e) => {
                      setSelectedDate(e.target.value);
                      setSelectedSlot(null);
                    }}
                    min={new Date().toISOString().split('T')[0]}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                  />
                </div>

                {selectedDate && selectedStaff === 'any' && (
                  <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <p className="text-sm text-yellow-800">
                      Please select a specific staff member to see available times, or contact us directly to book with the first available provider.
                    </p>
                  </div>
                )}

                {selectedDate && selectedStaff && selectedStaff !== 'any' && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Available Times
                    </label>
                    {availabilityLoading ? (
                      <div className="flex items-center justify-center py-8">
                        <Loader2 className="h-6 w-6 animate-spin text-primary-600" />
                      </div>
                    ) : availability && availability.length > 0 ? (
                      <div className="grid grid-cols-3 md:grid-cols-4 gap-2">
                        {availability.map((slot, index) => (
                          <button
                            key={index}
                            onClick={() => slot.isAvailable && setSelectedSlot(slot)}
                            disabled={!slot.isAvailable}
                            className={cn(
                              'p-2 rounded-md text-sm font-medium transition-colors',
                              !slot.isAvailable
                                ? 'bg-gray-100 text-gray-400 cursor-not-allowed dark:bg-gray-800 dark:text-gray-600'
                                : selectedSlot?.startTime === slot.startTime
                                ? 'bg-primary-600 text-white'
                                : 'bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700 text-gray-900 dark:text-white'
                            )}
                          >
                            {format(parseISO(slot.startTime), 'h:mm a')}
                          </button>
                        ))}
                      </div>
                    ) : (
                      <div className="text-center py-8 text-gray-500">
                        <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
                        <p>No available times for this date</p>
                        <p className="text-sm">Try selecting a different date</p>
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Client Details */}
            {currentStep === 'details' && (
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="First Name *"
                    value={clientDetails.firstName}
                    onChange={(e) => setClientDetails({ ...clientDetails, firstName: e.target.value })}
                    placeholder="John"
                  />
                  <Input
                    label="Last Name *"
                    value={clientDetails.lastName}
                    onChange={(e) => setClientDetails({ ...clientDetails, lastName: e.target.value })}
                    placeholder="Doe"
                  />
                </div>
                <Input
                  label="Email *"
                  type="email"
                  value={clientDetails.email}
                  onChange={(e) => setClientDetails({ ...clientDetails, email: e.target.value })}
                  placeholder="john@example.com"
                />
                <Input
                  label="Phone Number"
                  type="tel"
                  value={clientDetails.phone}
                  onChange={(e) => setClientDetails({ ...clientDetails, phone: e.target.value })}
                  placeholder="+1 (555) 123-4567"
                />
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Notes (optional)
                  </label>
                  <textarea
                    value={clientDetails.notes}
                    onChange={(e) => setClientDetails({ ...clientDetails, notes: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                    rows={3}
                    placeholder="Any special requests or information..."
                  />
                </div>
              </div>
            )}

            {/* Confirmation */}
            {currentStep === 'confirm' && (
              <div className="space-y-4">
                {bookMutation.error && (
                  <div className="p-4 bg-red-50 border border-red-200 rounded-lg flex items-start gap-3">
                    <AlertCircle className="h-5 w-5 text-red-600 mt-0.5" />
                    <div>
                      <p className="text-sm font-medium text-red-800">Booking Failed</p>
                      <p className="text-sm text-red-600">
                        {(bookMutation.error as Error).message || 'An error occurred. Please try again.'}
                      </p>
                    </div>
                  </div>
                )}

                <div className="p-4 rounded-lg bg-gray-50 dark:bg-gray-800 space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">Service</span>
                    <span className="font-medium text-gray-900 dark:text-white">
                      {selectedServiceData?.name}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">Staff</span>
                    <span className="font-medium text-gray-900 dark:text-white">
                      {selectedStaff === 'any' ? 'Any Available' : selectedStaffData?.fullName}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">Date & Time</span>
                    <span className="font-medium text-gray-900 dark:text-white">
                      {selectedSlot && format(parseISO(selectedSlot.startTime), 'MMM d, yyyy h:mm a')}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">Duration</span>
                    <span className="font-medium text-gray-900 dark:text-white">
                      {selectedServiceData && formatDuration(selectedServiceData.durationMinutes)}
                    </span>
                  </div>
                  <div className="border-t dark:border-gray-700 pt-3">
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Customer</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {clientDetails.firstName} {clientDetails.lastName}
                      </span>
                    </div>
                    <div className="flex justify-between mt-1">
                      <span className="text-gray-500 dark:text-gray-400">Email</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {clientDetails.email}
                      </span>
                    </div>
                  </div>
                  <div className="border-t dark:border-gray-700 pt-3 flex justify-between">
                    <span className="font-medium text-gray-900 dark:text-white">Total</span>
                    <span className="font-bold text-primary-600">
                      {selectedServiceData && formatCurrency(selectedServiceData.price)}
                    </span>
                  </div>
                </div>
              </div>
            )}

            {/* Navigation Buttons */}
            <div className="flex justify-between mt-6 pt-6 border-t dark:border-gray-700">
              {currentStepIndex > 0 ? (
                <Button variant="outline" onClick={goToPrev}>
                  Back
                </Button>
              ) : (
                <div />
              )}
              {currentStep === 'confirm' ? (
                <Button
                  onClick={handleBooking}
                  disabled={bookMutation.isPending}
                >
                  {bookMutation.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                      Booking...
                    </>
                  ) : (
                    'Confirm Booking'
                  )}
                </Button>
              ) : (
                <Button
                  onClick={goToNext}
                  disabled={!canContinue()}
                >
                  Continue
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
