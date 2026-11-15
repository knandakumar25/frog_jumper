import React, { createContext, useState, useEffect, useLayoutEffect, useRef, useContext, useCallback } from 'react';
import { useClerkSession } from '../auth/clerkGate';
import apiClient from '../api/client';
import { tokens } from '../api/tokens';

const AuthContext = createContext();

/** Holds the signed-in user only for this page lifetime; reload requires a new sign-in. */
export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const clerk = useClerkSession();
  const identity = clerk.isLoaded ? (clerk.isSignedIn ? clerk.userId : null) : undefined;
  const previousIdentity = useRef(undefined);
  const currentIdentity = useRef(identity);
  currentIdentity.current = identity;
  const epoch = useRef(0);
  const [version, setVersion] = useState(0);
  const ownedSetUser = useCallback(value => {
    if (currentIdentity.current === identity && epoch.current === version) setUser(value);
  }, [identity, version]);
  const identityChanged = identity !== undefined && identity !== previousIdentity.current;
  // This bridge remains mounted on public pages too. Signing out or changing
  // Clerk accounts must discard both API tokens and the old profile before paint.
  // A token-only emailed-onboarding tab has no Clerk identity and stays supported.
  useLayoutEffect(() => {
    if (identity === undefined) return;
    if (previousIdentity.current !== identity && (previousIdentity.current !== undefined || identity !== null)) {
      epoch.current += 1; setVersion(epoch.current);
      tokens.clear(); setUser(null);
    }
    previousIdentity.current = identity;
  }, [identity]);

  useEffect(() => {
    const loadUser = async () => {
      const token = tokens.access;
      if (token) {
        try {
          const response = await apiClient.get('/auth/profile/');
          if (currentIdentity.current === identity && epoch.current === version) setUser(response.data);
        } catch (error) {
          console.error('Failed to fetch user profile', error);
          if (currentIdentity.current === identity && epoch.current === version) tokens.clear();
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
    if (currentIdentity.current !== identity || epoch.current !== version) throw new Error('Session changed during sign-in');
    epoch.current += 1; setVersion(epoch.current);
    tokens.install(access, refresh);
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
    if (currentIdentity.current !== identity || epoch.current !== version) throw new Error('Session changed during sign-in');
    epoch.current += 1; setVersion(epoch.current);
    tokens.install(access, refresh);
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
    if (currentIdentity.current !== identity || epoch.current !== version) return false;
    epoch.current += 1; setVersion(epoch.current);
    tokens.install(access, refresh);
    setUser(userData);
    return true;
  };

  /** Discards this page’s credentials and user state. */
  const logout = () => {
    epoch.current += 1; setVersion(epoch.current);
    tokens.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user: identityChanged && (previousIdentity.current !== undefined || identity !== null) ? null : user, setUser: ownedSetUser, isLoading, login, verifyLogin2FA, register, loginWithTokens, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
