import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import type { ApiError } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';

class ApiClient {
  private instance: AxiosInstance;
  private accessToken: string | null = null;
  private tenantId: string | null = null;

  constructor() {
    this.instance = axios.create({
      baseURL: API_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor
    this.instance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        if (this.accessToken) {
          config.headers.Authorization = `Bearer ${this.accessToken}`;
        }
        if (this.tenantId) {
          config.headers['X-Tenant-Id'] = this.tenantId;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.instance.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<ApiError>) => {
        if (error.response?.status === 401) {
          // Could implement token refresh here
          this.clearAuth();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );

    // Load from localStorage on init
    this.loadFromStorage();
  }

  private loadFromStorage() {
    this.accessToken = localStorage.getItem('accessToken');
    this.tenantId = localStorage.getItem('tenantId');
  }

  setAuth(accessToken: string, refreshToken: string) {
    this.accessToken = accessToken;
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }

  clearAuth() {
    this.accessToken = null;
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  setTenant(tenantId: string) {
    this.tenantId = tenantId;
    localStorage.setItem('tenantId', tenantId);
  }

  clearTenant() {
    this.tenantId = null;
    localStorage.removeItem('tenantId');
  }

  get<T>(url: string, config = {}) {
    return this.instance.get<T>(url, config);
  }

  post<T>(url: string, data?: unknown, config = {}) {
    return this.instance.post<T>(url, data, config);
  }

  put<T>(url: string, data?: unknown, config = {}) {
    return this.instance.put<T>(url, data, config);
  }

  patch<T>(url: string, data?: unknown, config = {}) {
    return this.instance.patch<T>(url, data, config);
  }

  delete<T>(url: string, config = {}) {
    return this.instance.delete<T>(url, config);
  }
}

export const apiClient = new ApiClient();
