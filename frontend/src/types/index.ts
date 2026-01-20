// ============ Enums as const objects ============
export const AppointmentStatus = {
  Pending: 0,
  Confirmed: 1,
  InProgress: 2,
  Completed: 3,
  Cancelled: 4,
  NoShow: 5,
} as const;
export type AppointmentStatus = typeof AppointmentStatus[keyof typeof AppointmentStatus];

export const TenantRole = {
  Owner: 0,
  Admin: 1,
  Staff: 2,
  Receptionist: 3,
} as const;
export type TenantRole = typeof TenantRole[keyof typeof TenantRole];

export const SubscriptionPlan = {
  Free: 0,
  Basic: 1,
  Standard: 2,
  Professional: 3,
} as const;
export type SubscriptionPlan = typeof SubscriptionPlan[keyof typeof SubscriptionPlan];

// ============ Auth Types ============
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  tenants: TenantMembership[];
}

export interface TenantMembership {
  tenantId: string;
  tenantName: string;
  role: TenantRole;
}

// ============ Tenant Types ============
export interface Tenant {
  id: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  country?: string;
  timeZone: string;
  currency: string;
  plan: SubscriptionPlan;
  configJson?: TenantConfig;
}

export interface TenantConfig {
  primaryColor?: string;
  secondaryColor?: string;
  allowOnlineBooking?: boolean;
  requirePaymentUpfront?: boolean;
  cancellationPolicyHours?: number;
}

// ============ Service Types ============
export interface ServiceDefinition {
  id: string;
  name: string;
  description?: string;
  basePrice: number;
  baseDurationMinutes: number;
  capacity: number;
  bufferMinutes: number;
  color?: string;
  isActive: boolean;
  isGroupClass: boolean;
}

// ============ Staff Types ============
export interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  acceptsBookings: boolean;
  services: StaffServiceLink[];
}

export interface StaffServiceLink {
  serviceId: string;
  serviceName: string;
  priceOverride?: number;
  durationOverride?: number;
  effectivePrice: number;
  effectiveDuration: number;
}

// ============ Availability Types ============
export interface AvailabilityRequest {
  serviceId: string;
  staffId?: string;
  date: string; // YYYY-MM-DD
}

export interface TimeSlot {
  startTime: string; // ISO 8601
  endTime: string;
  available: boolean;
  remainingCapacity?: number;
  staffId?: string;
  staffName?: string;
}

export interface AvailabilityResponse {
  date: string;
  slots: TimeSlot[];
}

// ============ Appointment Types ============
export interface Appointment {
  id: string;
  serviceId: string;
  serviceName: string;
  staffMemberId: string;
  staffMemberName: string;
  clientId: string;
  clientName: string;
  startTimeUtc: string;
  endTimeUtc: string;
  status: AppointmentStatus;
  price: number;
  durationMinutes: number;
  internalNotes?: string;
  customerNotes?: string;
}

export interface CreateAppointmentRequest {
  serviceId: string;
  staffMemberId: string;
  startTimeUtc: string;
  customerNotes?: string;
}

// ============ Client Types ============
export interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  notes?: string;
  appointmentCount: number;
  lastVisitDate?: string;
}

// ============ API Response Types ============
export interface ApiError {
  status: number;
  message: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
