import React, { createContext, useState, useEffect, useContext } from 'react';
import apiClient from '../api/client';
import { tokens } from '../api/tokens';

const AuthContext = createContext();

/** Holds the signed-in user only for this page lifetime; reload requires a new sign-in. */
export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadUser = async () => {
      const token = tokens.access;
      if (token) {
        try {
          const response = await apiClient.get('/auth/profile/');
          setUser(response.data);
        } catch (error) {
          console.error('Failed to fetch user profile', error);
          tokens.clear();
        }
      }
      setIsLoading(false);
    };
    loadUser();
  }, []);

  /** Signs in through PostgreSQL-backed accounts and retains returned tokens in memory. */
  const login = async (username, password) => {
    const response = await apiClient.post('/auth/login/', { username, password });
    const data = response.data;

    // Handle 2FA requirement
    if (data.status === '2fa_required') {
      // Caller will handle verification; just return the payload
      return data;
    }

    const { access, refresh, user: userData } = data;
    tokens.access = access;
    tokens.refresh = refresh;
    setUser(userData);
    return data;
  };

  /** Exchanges a two-factor challenge for an in-memory authenticated session. */
  const verifyLogin2FA = async (loginSessionId, code) => {
    const response = await apiClient.post('/auth/2fa/verify-login/', {
      login_session_id: loginSessionId,
      code,
    });
    const { access, refresh, user: userData } = response.data;
    tokens.access = access;
    tokens.refresh = refresh;
    setUser(userData);
    return response.data;
  };


  /** Creates the server-side account without browser persistence. */
  const register = async (username, email, password, firstName = '', lastName = '', profilePicture = '', country = '') => {
    await apiClient.post('/auth/register/', {
      username,
      email,
      password,
      first_name: firstName,
      last_name: lastName,
      profile_picture: profilePicture,
      country
    });
  };

  // Used by the onboarding completion step, which receives a token pair from
  // /auth/onboarding/complete/ rather than from a login form. Kept here so the
  // storage contract lives in one place instead of being hand-written by a page.
  const loginWithTokens = (access, refresh, userData) => {
    tokens.access = access;
    tokens.refresh = refresh;
    setUser(userData);
  };

  /** Discards this page’s credentials and user state. */
  const logout = () => {
    tokens.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, setUser, isLoading, login, verifyLogin2FA, register, loginWithTokens, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
