import React, { createContext, useState, useEffect, useContext } from 'react';
import apiClient from '../api/client';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadUser = async () => {
      const token = localStorage.getItem('accessToken');
      if (token) {
        try {
          const response = await apiClient.get('/auth/profile/');
          setUser(response.data);
        } catch (error) {
          console.error('Failed to fetch user profile', error);
          localStorage.clear();
        }
      }
      setIsLoading(false);
    };
    loadUser();
  }, []);

  const login = async (username, password) => {
    const response = await apiClient.post('/auth/login/', { username, password });
    const data = response.data;

    // Handle 2FA requirement
    if (data.status === '2fa_required') {
      // Caller will handle verification; just return the payload
      return data;
    }

    const { access, refresh, user: userData } = data;
    localStorage.setItem('accessToken', access);
    localStorage.setItem('refreshToken', refresh);
    setUser(userData);
    return data;
  };

  const verifyLogin2FA = async (loginSessionId, code) => {
    const response = await apiClient.post('/auth/2fa/verify-login/', {
      login_session_id: loginSessionId,
      code,
    });
    const { access, refresh, user: userData } = response.data;
    localStorage.setItem('accessToken', access);
    localStorage.setItem('refreshToken', refresh);
    setUser(userData);
    return response.data;
  };


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
    localStorage.setItem('accessToken', access);
    localStorage.setItem('refreshToken', refresh);
    setUser(userData);
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, setUser, isLoading, login, verifyLogin2FA, register, loginWithTokens, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
