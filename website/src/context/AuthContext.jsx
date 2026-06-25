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
          const response = await apiClient.get('/profile/');
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
    const { access, refresh, user: userData } = response.data;
    localStorage.setItem('accessToken', access);
    localStorage.setItem('refreshToken', refresh);
    setUser(userData);
  };

  const register = async (username, email, password) => {
    await apiClient.post('/auth/register/', { username, email, password });
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, setUser, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
