/** Explicit browser approval for a public game request; Clerk owns all credential entry. */
import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { useClerkSession, useUser, useClerkClient, CLERK_ENABLED } from '../auth/clerkGate';
import { useAuth } from '../context/AuthContext';
import { tokens } from '../api/tokens';
import { API_BASE_URL } from '../api/client';
import { gameRequest } from '../auth/gameHandoff';

/** Keep public request state in navigation URLs; never persist tokens or device secrets. */
export default function BrowserAuth() {
  const { t } = useTranslation();
  const sid = gameRequest();
  const query = new URLSearchParams(window.location.search);
  const locale = query.get('locale') || 'en-US';
  const intent = query.get('intent') === 'signup' ? 'signup' : 'login';
  const { isLoaded, isSignedIn, getToken } = useClerkSession();
  const { user: clerkUser } = useUser();
  const { user: djangoUser, logout } = useAuth();
  const clerk = useClerkClient();
  const [status, setStatus] = useState('ready');
  const [message, setMessage] = useState('');
  const loginPath = `/${encodeURIComponent(locale)}/${intent}/game/${sid}`;

  /** Acquire fresh proof on each click, prefer Clerk identity over a stale Django profile. */
  async function confirm() {
    if (status === 'sending') return;
    setStatus('sending');
    try {
      const clerkToken = isSignedIn ? await getToken() : null;
      await axios.post(`${API_BASE_URL}/auth/browser-confirm/`, {
        session_id: sid, ...(clerkToken ? { clerk_token: clerkToken } : {}),
      }, { headers: !clerkToken && tokens.access ? { Authorization: `Bearer ${tokens.access}` } : {} });
      setStatus('authorized');
    } catch (error) {
      if (error.response?.data?.error === 'onboarding_required') {
        window.location.assign(`/onboarding?session_id=${sid}&locale=${encodeURIComponent(locale)}`);
        return;
      }
      setMessage(error.response?.status === 410 ? 'expired' : 'failed');
      setStatus('ready');
    }
  }

  /** Clear website memory and switch Clerk accounts while preserving this request's return route. */
  async function switchAccount() {
    logout();
    if (isSignedIn) await clerk.signOut({ redirectUrl: loginPath });
    else window.location.assign(loginPath);
  }

  return <section className="max-w-lg mx-auto my-32 p-8 border border-cyan-400 text-center" aria-live="polite">
    <h1 className="text-2xl mb-6">{t('auth.authorize_client', 'Authorize Game Client')}</h1>
    {!sid ? <p>{t('browserAuth.missing')}</p> :
      !isLoaded ? <p data-testid="browser-loading">{t('onboarding.loading', 'Loading...')}</p> :
      status === 'authorized' ? <p data-testid="browser-authorized">{t('showcase.returnGame')}</p> : <>
        {isSignedIn || djangoUser ? <>
          <p className="mb-6">{t('browserAuth.confirm', { account: clerkUser?.primaryEmailAddress?.emailAddress || clerkUser?.username || djangoUser?.username })}</p>
          <button data-testid="authorize-game" className="brutal-btn p-3" disabled={status === 'sending'} onClick={confirm}>{t('auth.authorize_now', 'Authorize Now')}</button>
          <button data-testid="switch-account" className="brutal-btn p-3 m-3" disabled={status === 'sending'} onClick={switchAccount}>{t('browserAuth.switchAccount')}</button>
        </> : <>
          <p className="mb-6">{t('auth.auth_required_desc', 'Please log in to your account to authorize the game client.')}</p>
          {CLERK_ENABLED ? <Link data-testid="browser-signin" className="brutal-btn p-3" to={loginPath}>{intent === 'signup' ? t('auth.sign_up', 'Sign Up') : t('auth.login_access_terminal', 'Login / Access Terminal')}</Link> : <p>{t('onboarding.not_configured', 'Onboarding is not configured on the server.')}</p>}
        </>}
        {message && <p role="alert" className="mt-6">{message === 'expired' ? t('browserAuth.expired') : t('auth.auth_failed_desc', 'An error occurred during authorization. Please try again.')}</p>}
      </>}
  </section>;
}
