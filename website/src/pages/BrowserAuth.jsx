import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

const BrowserAuth = () => {
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
          Authentication Required
        </h1>
        <p className="text-on-surface-variant mb-8 text-sm">
          Please log in to your account to authorize the game client.
        </p>
        <Link
          to={loginUrl}
          className="brutal-btn py-3.5 px-8 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95 inline-block"
        >
          Login / Access Terminal
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[50vh] p-8 text-center bg-surface-container-lowest border-2 border-primary-container/30 max-w-md mx-auto my-16 shadow-[0_0_20px_rgba(0,243,255,0.15)] relative">
      <h1 className="text-headline-md text-primary-container uppercase font-bold mb-6">
        Authorize Game Client
      </h1>
      
      {status === 'confirming' && (
        <div className="flex flex-col items-center gap-6">
          <p className="text-on-surface-variant text-sm leading-relaxed">
            The game client is requesting access to your account.
          </p>
          <button
            onClick={handleConfirm}
            className="brutal-btn py-3.5 px-8 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95"
          >
            Authorize Now
          </button>
        </div>
      )}
      
      {status === 'authorized' && (
        <div className="flex flex-col items-center gap-4">
          <span className="text-[48px] text-green-500 mb-2">
            ✓
          </span>
          <p className="font-bold text-lg text-green-400">
            Account Authorized Successfully!
          </p>
          <p className="text-xs text-on-surface-variant leading-relaxed">
            You can now return to the game client. This window can be safely closed.
          </p>
        </div>
      )}
      
      {status === 'error' && (
        <div className="flex flex-col items-center gap-4">
          <span className="text-[48px] text-red-500 mb-2">
            ✕
          </span>
          <p className="font-bold text-lg text-red-400">
            Authorization Failed
          </p>
          <p className="text-sm text-on-surface-variant mb-6 leading-relaxed">
            An error occurred during authorization. Please try again.
          </p>
          <button 
            onClick={() => setStatus('confirming')}
            className="brutal-btn py-2.5 px-6 font-bold uppercase text-error bg-error/10 hover:bg-error/20 transition-all duration-150 active:scale-95"
          >
            Try Again
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
