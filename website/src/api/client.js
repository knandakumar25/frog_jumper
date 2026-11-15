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

let accountProof = null;
/** Account pages acquire fresh Clerk proof per call; nothing is persisted. */
export function setAccountProof(getToken) {
  accountProof = getToken;
  return () => { if (accountProof === getToken) accountProof = null; };
}

// Interceptor to attach JWT token to requests
apiClient.interceptors.request.use(
  async (config) => {
    if (config._sessionGeneration !== undefined && config._sessionGeneration !== tokens.generation) throw new Error('Session changed before request');
    config._sessionGeneration = tokens.generation;
    if (accountProof && /^\/auth\/(?:profile(?:\/|$)|2fa\/)/.test(config.url || '') && !config.headers.Authorization) {
      const proof = await accountProof();
      if (config._sessionGeneration !== tokens.generation) throw new Error('Session changed while acquiring proof');
      if (!proof) throw new Error('Clerk session expired');
      config.headers.Authorization = `Clerk ${proof}`;
    }
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
let refreshGeneration = -1;

/** Trade the stored refresh token for a new pair once, however many requests are waiting. */
function refreshTokens() {
  const generation = tokens.generation;
  if (refreshInFlight && refreshGeneration === generation) return refreshInFlight;
  refreshGeneration = generation;
  const pending = axios.post(`${API_BASE_URL}/auth/token/refresh/`, { refresh: tokens.refresh })
    .then((response) => {
      if (tokens.generation !== generation) throw new Error('Session changed during refresh');
      tokens.access = response.data.access;
      if (response.data.refresh) tokens.refresh = response.data.refresh;
      return response.data.access;
    })
    .finally(() => { if (refreshInFlight === pending) refreshInFlight = null; });
  refreshInFlight = pending;
  return refreshInFlight;
}

// Interceptor to handle token refresh on 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !String(originalRequest.headers.Authorization || '').startsWith('Clerk ')) {
      if (originalRequest._sessionGeneration !== tokens.generation) return Promise.reject(error);
      originalRequest._retry = true;
      try {
        const access = await refreshTokens();
        if (originalRequest._sessionGeneration !== tokens.generation) throw new Error('Session changed before retry');
        originalRequest.headers.Authorization = `Bearer ${access}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        if (originalRequest._sessionGeneration !== tokens.generation) return Promise.reject(refreshError);
        tokens.clear();
        const current = new URL(window.location.href);
        const locale = current.searchParams.get('locale') || current.pathname.split('/')[1];
        const safeLocale = /^[a-z]{2}(?:[-_][a-z]{2})?$/i.test(locale) ? locale : 'en';
        const requestId = current.searchParams.get('session_id') || current.pathname.match(/\/game\/([0-9a-f-]{36})/i)?.[1];
        const suffix = /^[0-9a-f-]{36}$/i.test(requestId || '') ? `/game/${requestId}` : '';
        window.location.href = `/${safeLocale}/login${suffix}?expired=1`;
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
