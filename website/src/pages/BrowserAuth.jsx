import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

const BrowserAuth = () => {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const [sessionId, setSessionId] = useState(null);
  const [status, setStatus] = useState('idle'); // idle, confirming, authorized, error
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const sid = params.get('session_id');
    if (sid) {
      setSessionId(sid);
      setStatus('confirming');
    }
  }, []);

  const handleConfirm = async () => {
    try {
      await apiClient.post('/auth/browser-confirm/', { session_id: sessionId });
      setStatus('authorized');
    } catch (error) {
      setStatus('error');
    }
  };

  if (!user) {
    const loginUrl = `/${currentLocale}/login?redirect=${encodeURIComponent(window.location.pathname + window.location.search)}`;
    return (
      <div className="flex flex-col items-center justify-center min-h-[50vh] p-8 text-center bg-surface-container-lowest border-2 border-primary-container/30 max-w-md mx-auto my-16 shadow-[0_0_20px_rgba(0,243,255,0.15)] relative bracket-corners">
        <h1 className="text-headline-md text-primary-container uppercase font-bold mb-4">
          {t('auth.auth_required', 'Authentication Required')}
        </h1>
        <p className="text-on-surface-variant mb-8 text-sm">
          {t('auth.auth_required_desc', 'Please log in to your account to authorize the game client.')}
        </p>
        <Link
          to={loginUrl}
          className="brutal-btn py-3.5 px-8 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95 inline-block"
        >
          {t('auth.login_access_terminal', 'Login / Access Terminal')}
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[50vh] p-8 text-center bg-surface-container-lowest border-2 border-primary-container/30 max-w-md mx-auto my-16 shadow-[0_0_20px_rgba(0,243,255,0.15)] relative">
      <h1 className="text-headline-md text-primary-container uppercase font-bold mb-6">
        {t('auth.authorize_client', 'Authorize Game Client')}
      </h1>
      
      {status === 'confirming' && (
        <div className="flex flex-col items-center gap-6">
          <p className="text-on-surface-variant text-sm leading-relaxed">
            {t('auth.requesting_access', 'The game client is requesting access to your account.')}
          </p>
          <button
            onClick={handleConfirm}
            className="brutal-btn py-3.5 px-8 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95"
          >
            {t('auth.authorize_now', 'Authorize Now')}
          </button>
        </div>
      )}
      
      {status === 'authorized' && (
        <div className="flex flex-col items-center gap-4">
          <span className="text-[48px] text-green-500 mb-2">
            ✓
          </span>
          <p className="font-bold text-lg text-green-400">
            {t('auth.auth_success', 'Account Authorized Successfully!')}
          </p>
          <p className="text-xs text-on-surface-variant leading-relaxed">
            {t('auth.auth_success_desc', 'You can now return to the game client. This window can be safely closed.')}
          </p>
        </div>
      )}
      
      {status === 'error' && (
        <div className="flex flex-col items-center gap-4">
          <span className="text-[48px] text-red-500 mb-2">
            ✕
          </span>
          <p className="font-bold text-lg text-red-400">
            {t('auth.auth_failed', 'Authorization Failed')}
          </p>
          <p className="text-sm text-on-surface-variant mb-6 leading-relaxed">
            {t('auth.auth_failed_desc', 'An error occurred during authorization. Please try again.')}
          </p>
          <button 
            onClick={() => setStatus('confirming')}
            className="brutal-btn py-2.5 px-6 font-bold uppercase text-error bg-error/10 hover:bg-error/20 transition-all duration-150 active:scale-95"
          >
            {t('auth.try_again', 'Try Again')}
          </button>
        </div>
      )}
      
      {status === 'idle' && (
        <p className="text-on-surface-variant text-sm">
          No active session found for authorization.
        </p>
      )}
    </div>
  );
};

export default BrowserAuth;
