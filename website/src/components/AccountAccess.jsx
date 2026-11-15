import { useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { CLERK_ENABLED, useClerkSession } from '../auth/clerkGate';
import apiClient, { setAccountProof } from '../api/client';
import { Page, useLocalePath } from './Showcase';

/** Resolve linked account state with verified proof; never authorize a game here. */
export default function AccountAccess({ children }) {
  const { user, setUser, logout, isLoading } = useAuth();
  const session = useClerkSession();
  const { t } = useTranslation();
  const base = useLocalePath();
  const location = useLocation();
  const [phase, setPhase] = useState('loading');
  const [attempt, setAttempt] = useState(0);
  const previousIdentity = useRef(null);
  const identity = session.isSignedIn ? session.userId : null;
  useEffect(() => {
    if (!session.isLoaded) return;
    if (!session.isSignedIn) {
      if (previousIdentity.current) logout();
      previousIdentity.current = null;
      setPhase('signedOut');
      return;
    }
    let active = true;
    setPhase('loading');
    if (previousIdentity.current && previousIdentity.current !== identity) logout();
    previousIdentity.current = identity;
    const clear = setAccountProof(session.getToken);
    apiClient.get('/auth/profile/').then(({data}) => {
      if (active) { setUser(data); setPhase('ready'); }
    }).catch(error => {
      if (active) { setUser(null); setPhase(error.response?.data?.error === 'onboarding_required' ? 'onboarding' : error.response?.status === 401 ? 'expired' : 'unavailable'); }
    });
    return () => { active = false; clear(); };
    // AuthContext actions are recreated on render; identity, proof and retry own this request.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session.isLoaded, session.isSignedIn, identity, session.getToken, attempt, setUser]);
  if (isLoading || !session.isLoaded || (session.isSignedIn && phase === 'loading')) return <Page title={t('showcase.account')}><p role="status">{t('showcase.loading')}</p></Page>;
  if (user && (!session.isSignedIn || phase === 'ready')) return children;
  const expired = phase === 'expired' || new URLSearchParams(location.search).has('expired');
  return <Page title={t('showcase.account')}>
    {phase === 'onboarding' ? <><p>{t('showcase.onboarding')}</p><Link className="button" to={`/onboarding?locale=${encodeURIComponent(base.slice(1))}`}>{t('showcase.onboarding')}</Link></> : phase === 'unavailable' ? <><p role="alert">{t('showcase.unavailable')}</p><button className="button" onClick={() => setAttempt(attempt+1)}>{t('showcase.requestAgain')}</button></> : <><p>{!CLERK_ENABLED ? t('showcase.notConfigured') : expired ? t('showcase.sessionExpired') : t('showcase.signedOut')}</p><Link className="button" to={`${base}/login`}>{t('showcase.login')}</Link></>}
  </Page>;
}
