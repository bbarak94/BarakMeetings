import { apiClient } from './client';

export const TenantRole = {
  Owner: 0,
  Admin: 1,
  Staff: 2,
  Receptionist: 3,
} as const;

export type TenantRoleType = (typeof TenantRole)[keyof typeof TenantRole];

export const TenantRoleLabels: Record<number, string> = {
  0: 'Owner',
  1: 'Admin',
  2: 'Staff',
  3: 'Receptionist',
};

export interface TenantUserDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: TenantRoleType;
  isActive: boolean;
  isStaff: boolean;
  staffMemberId?: string;
  lastLoginAt?: string;
}

export interface InviteUserRequest {
  email: string;
  role?: TenantRoleType;
  createAsStaff?: boolean;
  staffTitle?: string;
}

export interface InvitationDto {
  id: string;
  email: string;
  role: TenantRoleType;
  createAsStaff: boolean;
  staffTitle?: string;
  invitedByName: string;
  createdAt: string;
  expiresAt: string;
  invitationLink?: string; // Only included when email is disabled (development)
}

export interface UpdateRoleRequest {
  role: TenantRoleType;
}

export const usersApi = {
  getAll: async (): Promise<TenantUserDto[]> => {
    const response = await apiClient.get<TenantUserDto[]>('/users');
    return response.data;
  },

  getById: async (id: string): Promise<TenantUserDto> => {
    const response = await apiClient.get<TenantUserDto>(`/users/${id}`);
    return response.data;
  },

  invite: async (data: InviteUserRequest): Promise<InvitationDto> => {
    const response = await apiClient.post<InvitationDto>('/users/invite', data);
    return response.data;
  },

  getPendingInvitations: async (): Promise<InvitationDto[]> => {
    const response = await apiClient.get<InvitationDto[]>('/users/invitations');
    return response.data;
  },

  resendInvitation: async (id: string): Promise<void> => {
    await apiClient.post(`/users/invitations/${id}/resend`);
  },

  cancelInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/invitations/${id}`);
  },

  updateRole: async (id: string, data: UpdateRoleRequest): Promise<void> => {
    await apiClient.put(`/users/${id}/role`, data);
  },

  deactivate: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },

  activate: async (id: string): Promise<void> => {
    await apiClient.post(`/users/${id}/activate`);
  },
};
