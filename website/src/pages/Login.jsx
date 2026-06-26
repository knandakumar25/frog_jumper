import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login, setUser } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  // Extract redirect parameter
  const params = new URLSearchParams(location.search);
  const redirectTarget = params.get('redirect');



  const handleEpicLogin = () => {
    setIsSubmitting(true);
    const epicLoginUrl = `http://localhost:8000/api/auth/epic/login/${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`;
    window.location.href = epicLoginUrl;
  };



  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');
    setIsSubmitting(true);

    try {
      await login(username, password);
      if (redirectTarget) {
        navigate(redirectTarget);
      } else {
        navigate(`/${currentLocale}`);
      }
    } catch (err) {
      console.error(err);
      setErrorMsg('Authentication failed. Please verify your credentials.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="w-full max-w-md bg-surface-container-lowest border-2 border-primary-container/30 p-8 shadow-[0_0_30px_rgba(0,243,255,0.15)] relative bracket-corners">
        
        {/* Accent corners */}
        <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-primary-container"></div>
        <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-primary-container"></div>

        <h2 className="text-headline-lg font-headline-lg text-primary-container uppercase mb-8 text-center font-extrabold tracking-widest drop-shadow-[0_0_10px_rgba(0,243,255,0.4)]">
          USER LOGIN
        </h2>

        {errorMsg && (
          <div className="bg-error/15 border border-error/50 p-4 mb-6 text-error text-sm font-semibold flex items-center gap-2">
            <span className="material-symbols-outlined text-[20px]">warning</span>
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              Username
            </label>
            <input
              type="text"
              placeholder="Enter your username"
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              Password
            </label>
            <input
              type="password"
              placeholder="••••••••"
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 hover:skew-x-[-8deg] active:scale-95 disabled:opacity-50 mt-2"
          >
            {isSubmitting ? 'Authenticating...' : 'Access Terminal'}
          </button>
        </form>

        <div className="flex items-center gap-3 my-5">
          <div className="h-[1px] bg-outline-variant/30 flex-grow"></div>
          <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant/60">OR</span>
          <div className="h-[1px] bg-outline-variant/30 flex-grow"></div>
        </div>

        <div className="flex flex-col gap-3 w-full">
          <button
            type="button"
            onClick={handleEpicLogin}
            disabled={isSubmitting}
            className="flex items-center justify-center gap-3 w-full py-2.5 px-4 bg-[#313131]/20 hover:bg-[#313131]/45 border-2 border-[#313131]/40 hover:border-[#313131]/70 text-on-surface font-bold text-sm tracking-wider uppercase transition-all duration-150 hover:skew-x-[-4deg] active:scale-95 disabled:opacity-50"
          >
            <svg className="w-4 h-4 fill-on-surface" viewBox="0 0 24 24">
              <path d="M12 .002L1.832 3.18C1.458 3.296 1.2 3.64 1.2 4.032V12.18c0 5.485 3.929 10.395 10.4 11.82a.82.82 0 0 0 .4 0c6.471-1.425 10.4-6.335 10.4-11.82V4.032c0-.392-.258-.736-.632-.852L12 .002zm5.798 12.178c0 3.493-2.316 6.643-5.798 7.643c-3.482-1-5.798-4.15-5.798-7.643V5.986l5.798-1.812l5.798 1.812v6.194z" />
            </svg>
            <span>Epic Games Login</span>
          </button>
        </div>

        <div className="mt-8 text-center text-xs font-body-md border-t border-primary-container/10 pt-6">
          <span className="text-on-surface-variant">Need an account? </span>
          <Link
            to={`/${currentLocale}/signup${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`}
            className="text-primary-container hover:text-secondary-container font-bold underline transition-colors"
          >
            Register / Create Account
          </Link>
        </div>
      </div>
    </div>
  );
}
