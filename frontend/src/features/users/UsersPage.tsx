import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Loader2, User, Shield, X, UserCheck, UserX, Mail, RefreshCw, Trash2, Clock } from 'lucide-react';
import { Button, Card, CardContent, CardHeader, CardTitle, Input } from '../../components/ui';
import { usersApi, type InviteUserRequest, type InvitationDto, TenantRole, TenantRoleLabels, type TenantRoleType } from '../../api/users';
import { useAuthStore } from '../../stores/authStore';
import { format, parseISO } from 'date-fns';

interface InviteFormData {
  email: string;
  role: TenantRoleType;
  createAsStaff: boolean;
  staffTitle: string;
}

const defaultInviteForm: InviteFormData = {
  email: '',
  role: TenantRole.Staff,
  createAsStaff: true,
  staffTitle: '',
};

export function UsersPage() {
  const queryClient = useQueryClient();
  const { user: currentUser } = useAuthStore();
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteForm, setInviteForm] = useState<InviteFormData>(defaultInviteForm);
  const [selectedUser, setSelectedUser] = useState<string | null>(null);
  const [newRole, setNewRole] = useState<TenantRoleType | null>(null);
  const [inviteSent, setInviteSent] = useState(false);

  const { data: users, isLoading } = useQuery({
    queryKey: ['tenant-users'],
    queryFn: () => usersApi.getAll(),
  });

  const { data: pendingInvitations } = useQuery({
    queryKey: ['pending-invitations'],
    queryFn: () => usersApi.getPendingInvitations(),
  });

  const inviteMutation = useMutation({
    mutationFn: (data: InviteUserRequest) => usersApi.invite(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-invitations'] });
      setInviteSent(true);
    },
  });

  const resendMutation = useMutation({
    mutationFn: (id: string) => usersApi.resendInvitation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-invitations'] });
    },
  });

  const cancelInvitationMutation = useMutation({
    mutationFn: (id: string) => usersApi.cancelInvitation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-invitations'] });
    },
  });

  const updateRoleMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: TenantRoleType }) =>
      usersApi.updateRole(id, { role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-users'] });
      setSelectedUser(null);
      setNewRole(null);
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => usersApi.deactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-users'] });
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => usersApi.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-users'] });
    },
  });

  const closeInviteModal = () => {
    setIsInviteModalOpen(false);
    setInviteForm(defaultInviteForm);
    setInviteSent(false);
  };

  const handleInvite = (e: React.FormEvent) => {
    e.preventDefault();
    inviteMutation.mutate({
      email: inviteForm.email,
      role: inviteForm.role,
      createAsStaff: inviteForm.createAsStaff && inviteForm.role === TenantRole.Staff,
      staffTitle: inviteForm.staffTitle || undefined,
    });
  };

  const getRoleBadgeColor = (role: TenantRoleType) => {
    switch (role) {
      case TenantRole.Owner:
        return 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300';
      case TenantRole.Admin:
        return 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300';
      case TenantRole.Staff:
        return 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300';
      case TenantRole.Receptionist:
        return 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300';
      default:
        return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Team Members</h1>
          <p className="text-gray-500 dark:text-gray-400">
            Manage user access and permissions
          </p>
        </div>
        <Button onClick={() => setIsInviteModalOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Invite User
        </Button>
      </div>

      {/* Role Legend */}
      <div className="flex flex-wrap gap-2">
        {Object.entries(TenantRoleLabels).map(([value, label]) => (
          <span
            key={value}
            className={`px-2 py-1 text-xs rounded-full ${getRoleBadgeColor(parseInt(value) as TenantRoleType)}`}
          >
            {label}
          </span>
        ))}
      </div>

      {/* Pending Invitations */}
      {pendingInvitations && pendingInvitations.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Clock className="h-5 w-5" />
              Pending Invitations
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b dark:border-gray-700">
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">Email</th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">Role</th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">Invited By</th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">Expires</th>
                    <th className="text-right p-4 font-medium text-gray-500 dark:text-gray-400">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {pendingInvitations.map((invitation: InvitationDto) => (
                    <tr key={invitation.id} className="border-b dark:border-gray-700 last:border-0">
                      <td className="p-4">
                        <div className="flex items-center gap-2">
                          <Mail className="h-4 w-4 text-gray-400" />
                          <span className="text-gray-900 dark:text-white">{invitation.email}</span>
                        </div>
                      </td>
                      <td className="p-4">
                        <span className={`px-2 py-1 text-xs rounded-full ${getRoleBadgeColor(invitation.role)}`}>
                          {TenantRoleLabels[invitation.role]}
                        </span>
                      </td>
                      <td className="p-4 text-sm text-gray-500">{invitation.invitedByName}</td>
                      <td className="p-4 text-sm text-gray-500">
                        {format(parseISO(invitation.expiresAt), 'MMM d, yyyy')}
                      </td>
                      <td className="p-4">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => resendMutation.mutate(invitation.id)}
                            disabled={resendMutation.isPending}
                            title="Resend invitation"
                          >
                            {resendMutation.isPending ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <RefreshCw className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => cancelInvitationMutation.mutate(invitation.id)}
                            disabled={cancelInvitationMutation.isPending}
                            title="Cancel invitation"
                          >
                            {cancelInvitationMutation.isPending ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Trash2 className="h-4 w-4 text-red-500" />
                            )}
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Users List */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Active Members</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
            </div>
          ) : users && users.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b dark:border-gray-700">
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      User
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Role
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Status
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Last Login
                    </th>
                    <th className="text-right p-4 font-medium text-gray-500 dark:text-gray-400">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((member) => (
                    <tr key={member.id} className="border-b dark:border-gray-700 last:border-0">
                      <td className="p-4">
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-primary-100 flex items-center justify-center">
                            <span className="text-sm font-medium text-primary-700">
                              {member.firstName[0]}
                              {member.lastName[0]}
                            </span>
                          </div>
                          <div>
                            <p className="font-medium text-gray-900 dark:text-white">
                              {member.fullName}
                              {member.userId === currentUser?.id && (
                                <span className="ml-2 text-xs text-gray-500">(You)</span>
                              )}
                            </p>
                            <p className="text-sm text-gray-500">{member.email}</p>
                          </div>
                        </div>
                      </td>
                      <td className="p-4">
                        {selectedUser === member.id ? (
                          <div className="flex items-center gap-2">
                            <select
                              value={newRole ?? member.role}
                              onChange={(e) => setNewRole(parseInt(e.target.value) as TenantRoleType)}
                              className="text-sm border border-gray-300 rounded px-2 py-1 dark:bg-gray-800 dark:border-gray-600"
                            >
                              {Object.entries(TenantRoleLabels).map(([value, label]) => (
                                <option key={value} value={value}>
                                  {label}
                                </option>
                              ))}
                            </select>
                            <Button
                              size="sm"
                              onClick={() => {
                                if (newRole !== null && newRole !== member.role) {
                                  updateRoleMutation.mutate({ id: member.id, role: newRole });
                                } else {
                                  setSelectedUser(null);
                                }
                              }}
                              disabled={updateRoleMutation.isPending}
                            >
                              {updateRoleMutation.isPending ? (
                                <Loader2 className="h-3 w-3 animate-spin" />
                              ) : (
                                'Save'
                              )}
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => {
                                setSelectedUser(null);
                                setNewRole(null);
                              }}
                            >
                              <X className="h-3 w-3" />
                            </Button>
                          </div>
                        ) : (
                          <span
                            className={`px-2 py-1 text-xs rounded-full ${getRoleBadgeColor(member.role)}`}
                          >
                            {TenantRoleLabels[member.role]}
                          </span>
                        )}
                      </td>
                      <td className="p-4">
                        <div className="flex items-center gap-2">
                          {member.isActive ? (
                            <span className="px-2 py-1 text-xs rounded-full bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
                              Active
                            </span>
                          ) : (
                            <span className="px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300">
                              Inactive
                            </span>
                          )}
                          {member.isStaff && (
                            <span className="px-2 py-1 text-xs rounded-full bg-blue-100 text-blue-700">
                              Staff
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="p-4 text-sm text-gray-500">
                        {member.lastLoginAt
                          ? format(parseISO(member.lastLoginAt), 'MMM d, yyyy')
                          : 'Never'}
                      </td>
                      <td className="p-4">
                        <div className="flex items-center justify-end gap-2">
                          {member.userId !== currentUser?.id && (
                            <>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  setSelectedUser(member.id);
                                  setNewRole(member.role);
                                }}
                                title="Change role"
                              >
                                <Shield className="h-4 w-4" />
                              </Button>
                              {member.isActive ? (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => deactivateMutation.mutate(member.id)}
                                  disabled={deactivateMutation.isPending}
                                  title="Deactivate"
                                >
                                  {deactivateMutation.isPending ? (
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                  ) : (
                                    <UserX className="h-4 w-4 text-red-500" />
                                  )}
                                </Button>
                              ) : (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => activateMutation.mutate(member.id)}
                                  disabled={activateMutation.isPending}
                                  title="Activate"
                                >
                                  {activateMutation.isPending ? (
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                  ) : (
                                    <UserCheck className="h-4 w-4 text-green-500" />
                                  )}
                                </Button>
                              )}
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-12 text-gray-500">
              <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No team members found</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Invite Modal */}
      {isInviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={closeInviteModal}
          />
          <Card className="relative z-10 w-full max-w-lg mx-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>{inviteSent ? 'Invitation Sent!' : 'Invite Team Member'}</CardTitle>
              <Button variant="ghost" size="sm" onClick={closeInviteModal}>
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <CardContent>
              {inviteSent ? (
                <div className="text-center py-4">
                  <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                    <Mail className="h-8 w-8 text-green-600" />
                  </div>
                  <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                    Invitation Sent!
                  </h3>
                  <p className="text-gray-600 dark:text-gray-400 mb-4">
                    An email has been sent to <strong>{inviteForm.email}</strong> with instructions to join your team.
                  </p>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">
                    The invitation will expire in 7 days.
                  </p>
                  <div className="flex justify-center gap-2">
                    <Button variant="outline" onClick={() => {
                      setInviteForm(defaultInviteForm);
                      setInviteSent(false);
                    }}>
                      Invite Another
                    </Button>
                    <Button onClick={closeInviteModal}>
                      Done
                    </Button>
                  </div>
                </div>
              ) : (
                <form onSubmit={handleInvite} className="space-y-4">
                  <Input
                    label="Email Address *"
                    type="email"
                    value={inviteForm.email}
                    onChange={(e) => setInviteForm({ ...inviteForm, email: e.target.value })}
                    placeholder="john@example.com"
                    required
                  />

                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Role *
                    </label>
                    <select
                      value={inviteForm.role}
                      onChange={(e) => setInviteForm({ ...inviteForm, role: parseInt(e.target.value) as TenantRoleType })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                    >
                      <option value={TenantRole.Staff}>Staff - Can manage appointments</option>
                      <option value={TenantRole.Receptionist}>Receptionist - Can view and book</option>
                      <option value={TenantRole.Admin}>Admin - Full access</option>
                      <option value={TenantRole.Owner}>Owner - Full access + billing</option>
                    </select>
                  </div>

                  {inviteForm.role === TenantRole.Staff && (
                    <>
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={inviteForm.createAsStaff}
                          onChange={(e) => setInviteForm({ ...inviteForm, createAsStaff: e.target.checked })}
                          className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                        />
                        <span className="text-sm text-gray-700 dark:text-gray-300">
                          Create as bookable staff member
                        </span>
                      </label>

                      {inviteForm.createAsStaff && (
                        <Input
                          label="Staff Title (optional)"
                          value={inviteForm.staffTitle}
                          onChange={(e) => setInviteForm({ ...inviteForm, staffTitle: e.target.value })}
                          placeholder="e.g., Senior Stylist"
                        />
                      )}
                    </>
                  )}

                  <div className="bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg text-sm text-blue-800 dark:text-blue-200">
                    <p>
                      An invitation email will be sent with a link for them to create their account and join your team.
                    </p>
                  </div>

                  {inviteMutation.isError && (
                    <div className="p-3 bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 rounded-lg text-sm">
                      {(inviteMutation.error as Error)?.message || 'Failed to send invitation'}
                    </div>
                  )}

                  <div className="flex justify-end gap-2 pt-4">
                    <Button type="button" variant="outline" onClick={closeInviteModal}>
                      Cancel
                    </Button>
                    <Button
                      type="submit"
                      disabled={inviteMutation.isPending || !inviteForm.email}
                    >
                      {inviteMutation.isPending ? (
                        <>
                          <Loader2 className="h-4 w-4 animate-spin mr-2" />
                          Sending...
                        </>
                      ) : (
                        'Send Invitation'
                      )}
                    </Button>
                  </div>
                </form>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
