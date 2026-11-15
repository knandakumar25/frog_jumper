/** HTTP client with process-memory credentials; browser storage is never read or written. */
import axios from 'axios';
import { tokens } from './tokens';

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8000/api';
export const BACKEND_URL = API_BASE_URL.replace(/\/api$/, '');

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor to attach JWT token to requests
apiClient.interceptors.request.use(
  (config) => {
    const token = tokens.access;
    if (token && !config.headers.Authorization) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// The refresh in progress, shared by every request that got a 401 meanwhile. Refresh tokens
// work once (backend change, 2026-11-15): two parallel refreshes with the same token would make
// the second fail and sign the user out, so all waiting requests reuse one refresh.
let refreshInFlight = null;

/** Trade the stored refresh token for a new pair once, however many requests are waiting. */
function refreshTokens() {
  refreshInFlight ??= axios.post(`${API_BASE_URL}/auth/token/refresh/`, { refresh: tokens.refresh })
    .then((response) => {
      tokens.access = response.data.access;
      if (response.data.refresh) tokens.refresh = response.data.refresh;
      return response.data.access;
    })
    .finally(() => { refreshInFlight = null; });
  return refreshInFlight;
}

// Interceptor to handle token refresh on 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const access = await refreshTokens();
        originalRequest.headers.Authorization = `Bearer ${access}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        tokens.clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
