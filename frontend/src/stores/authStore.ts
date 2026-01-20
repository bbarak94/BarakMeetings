import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { apiClient } from '../api/client';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  tenantId?: string;
  tenantName?: string;
  role?: string;
}

interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  setAuth: (user: User, token: string, refreshToken: string) => void;
  setUser: (user: User | null) => void;
  logout: () => void;
  // Compatibility with old code
  currentTenant: { tenantId: string; tenantName: string; role: string } | null;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      refreshToken: null,
      isAuthenticated: false,
      currentTenant: null,

      setAuth: (user, token, refreshToken) => {
        // Sync with API client
        apiClient.setAuth(token, refreshToken);
        if (user.tenantId) {
          apiClient.setTenant(user.tenantId);
        }
        set({
          user,
          token,
          refreshToken,
          isAuthenticated: true,
          currentTenant: user.tenantId
            ? {
                tenantId: user.tenantId,
                tenantName: user.tenantName || '',
                role: user.role || '',
              }
            : null,
        });
      },

      setUser: (user) =>
        set({
          user,
          isAuthenticated: !!user,
          currentTenant: user?.tenantId
            ? {
                tenantId: user.tenantId,
                tenantName: user.tenantName || '',
                role: user.role || '',
              }
            : null,
        }),

      logout: () => {
        // Clear API client auth
        apiClient.clearAuth();
        apiClient.clearTenant();
        set({
          user: null,
          token: null,
          refreshToken: null,
          currentTenant: null,
          isAuthenticated: false,
        });
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        refreshToken: state.refreshToken,
        currentTenant: state.currentTenant,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        // When store rehydrates from localStorage, sync with API client
        if (state?.token && state?.refreshToken) {
          apiClient.setAuth(state.token, state.refreshToken);
        }
        if (state?.currentTenant?.tenantId) {
          apiClient.setTenant(state.currentTenant.tenantId);
        }
      },
    }
  )
);
