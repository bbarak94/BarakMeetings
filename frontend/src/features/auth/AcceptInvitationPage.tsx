import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { Loader2, CheckCircle, XCircle, Mail, Building2, User } from 'lucide-react';
import { apiClient } from '../../api/client';
import { useAuthStore } from '../../stores/authStore';
import { Button, Input, Card, CardContent, CardHeader, CardTitle } from '../../components/ui';

interface InvitationInfo {
  email: string;
  tenantName: string;
  invitedByName: string;
  role: string;
  expiresAt: string;
  userExists: boolean;
}

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    tenantId: string;
    tenantName: string;
    role: string;
  };
}

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();
  const token = searchParams.get('token');

  const [status, setStatus] = useState<'loading' | 'valid' | 'invalid' | 'expired' | 'accepted'>('loading');
  const [invitation, setInvitation] = useState<InvitationInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    password: '',
    confirmPassword: '',
  });

  useEffect(() => {
    if (!token) {
      setStatus('invalid');
      setError('No invitation token provided');
      return;
    }

    // Fetch invitation info
    apiClient
      .get<InvitationInfo>(`/Auth/invitation/${token}`)
      .then((res) => {
        setInvitation(res.data);
        setStatus('valid');
      })
      .catch((err) => {
        const message = err.response?.data?.message || 'Invalid invitation';
        if (message.includes('expired')) {
          setStatus('expired');
        } else {
          setStatus('invalid');
        }
        setError(message);
      });
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!invitation?.userExists) {
      if (!formData.firstName || !formData.lastName) {
        setError('Please enter your first and last name');
        return;
      }
    }

    if (!formData.password) {
      setError('Please enter a password');
      return;
    }

    if (!invitation?.userExists && formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (formData.password.length < 6) {
      setError('Password must be at least 6 characters');
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await apiClient.post<TokenResponse>(`/Auth/invitation/${token}/accept`, {
        password: formData.password,
        firstName: formData.firstName || undefined,
        lastName: formData.lastName || undefined,
      });

      const { accessToken, refreshToken, user } = response.data;

      setAuth(
        {
          id: user.id,
          email: user.email,
          firstName: user.firstName,
          lastName: user.lastName,
          tenantId: user.tenantId,
          tenantName: user.tenantName,
          role: user.role,
        },
        accessToken,
        refreshToken
      );

      setStatus('accepted');

      // Redirect to dashboard after a short delay
      setTimeout(() => {
        navigate('/dashboard');
      }, 2000);
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { message?: string } } };
      setError(axiosError.response?.data?.message || 'Failed to accept invitation');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (status === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
        <div className="text-center">
          <Loader2 className="h-12 w-12 animate-spin text-primary-600 mx-auto" />
          <p className="mt-4 text-gray-600 dark:text-gray-400">Validating invitation...</p>
        </div>
      </div>
    );
  }

  if (status === 'invalid' || status === 'expired') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6 text-center">
            <XCircle className="h-16 w-16 text-red-500 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
              {status === 'expired' ? 'Invitation Expired' : 'Invalid Invitation'}
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-6">{error}</p>
            <Link to="/login">
              <Button variant="outline">Go to Login</Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (status === 'accepted') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6 text-center">
            <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">Welcome!</h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              You've successfully joined {invitation?.tenantName}
            </p>
            <p className="text-sm text-gray-500">Redirecting to dashboard...</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Accept Invitation</CardTitle>
        </CardHeader>
        <CardContent>
          {/* Invitation Info */}
          <div className="bg-primary-50 dark:bg-primary-900/20 rounded-lg p-4 mb-6 space-y-3">
            <div className="flex items-center gap-3">
              <Building2 className="h-5 w-5 text-primary-600" />
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Business</p>
                <p className="font-medium text-gray-900 dark:text-white">{invitation?.tenantName}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <User className="h-5 w-5 text-primary-600" />
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Invited by</p>
                <p className="font-medium text-gray-900 dark:text-white">{invitation?.invitedByName}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Mail className="h-5 w-5 text-primary-600" />
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">Your email</p>
                <p className="font-medium text-gray-900 dark:text-white">{invitation?.email}</p>
              </div>
            </div>
            <div className="pt-2 border-t border-primary-200 dark:border-primary-800">
              <p className="text-sm text-primary-700 dark:text-primary-300">
                You will join as: <strong>{invitation?.role}</strong>
              </p>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            {!invitation?.userExists && (
              <>
                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="First Name"
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    placeholder="John"
                    required
                  />
                  <Input
                    label="Last Name"
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    placeholder="Doe"
                    required
                  />
                </div>
              </>
            )}

            <Input
              label={invitation?.userExists ? 'Enter your password' : 'Create a password'}
              type="password"
              value={formData.password}
              onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              placeholder="Enter password"
              required
            />

            {!invitation?.userExists && (
              <Input
                label="Confirm Password"
                type="password"
                value={formData.confirmPassword}
                onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
                placeholder="Confirm password"
                required
              />
            )}

            {error && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 rounded-lg text-sm">
                {error}
              </div>
            )}

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  {invitation?.userExists ? 'Joining...' : 'Creating Account...'}
                </>
              ) : invitation?.userExists ? (
                'Join Team'
              ) : (
                'Create Account & Join'
              )}
            </Button>
          </form>

          {invitation?.userExists && (
            <p className="mt-4 text-center text-sm text-gray-500 dark:text-gray-400">
              Already have an account? Enter your existing password to join this business.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
