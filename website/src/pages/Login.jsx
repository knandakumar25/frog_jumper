import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient, { BACKEND_URL } from '../api/client';

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
    const epicLoginUrl = `${BACKEND_URL}/api/auth/epic/login/${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`;
    window.location.href = epicLoginUrl;
  };

  const handleDiscordLogin = () => {
    setIsSubmitting(true);
    const discordLoginUrl = `${BACKEND_URL}/api/auth/discord/login/${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`;
    window.location.href = discordLoginUrl;
  };

  const handleSteamLogin = () => {
    setIsSubmitting(true);
    const steamLoginUrl = `${BACKEND_URL}/api/auth/steam/login/${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`;
    window.location.href = steamLoginUrl;
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

          <button
            type="button"
            onClick={handleSteamLogin}
            disabled={isSubmitting}
            className="flex items-center justify-center gap-3 w-full py-2.5 px-4 bg-[#1b2838]/30 hover:bg-[#1b2838]/60 border-2 border-[#66c0f4]/20 hover:border-[#66c0f4]/50 text-on-surface font-bold text-sm tracking-wider uppercase transition-all duration-150 hover:skew-x-[-4deg] active:scale-95 disabled:opacity-50"
          >
            <svg className="w-4 h-4 fill-on-surface" viewBox="0 0 24 24">
              <path d="M12 0C5.38 0 0 5.38 0 12s5.38 12 12 12 12-5.38 12-12S18.62 0 12 0zm0 1.25c5.94 0 10.75 4.81 10.75 10.75S17.94 22.75 12 22.75c-4.48 0-8.32-2.73-9.92-6.62l5.03-2.07c.64.44 1.41.7 2.25.7 2.21 0 4-1.79 4-4s-1.79-4-4-4c-1.34 0-2.52.66-3.25 1.67L3.08 7.42C5.03 3.72 8.92 1.25 12 1.25zm-2.64 8.7c1.3 0 2.35 1.05 2.35 2.35s-1.05 2.35-2.35 2.35-2.35-1.05-2.35-2.35 1.05-2.35 2.35-2.35z"/>
            </svg>
            <span>Steam Login</span>
          </button>

          <button
            type="button"
            onClick={handleDiscordLogin}
            disabled={isSubmitting}
            className="flex items-center justify-center gap-3 w-full py-2.5 px-4 bg-[#5865F2]/15 hover:bg-[#5865F2]/30 border-2 border-[#5865F2]/30 hover:border-[#5865F2]/60 text-on-surface font-bold text-sm tracking-wider uppercase transition-all duration-150 hover:skew-x-[-4deg] active:scale-95 disabled:opacity-50"
          >
            <svg className="w-4 h-4 fill-on-surface" viewBox="0 0 24 24">
              <path d="M19.27 4.73a16.14 16.14 0 0 0-3.97-1.23a.1.1 0 0 0-.1.05a11.23 11.23 0 0 0-.5 1.02a14.97 14.97 0 0 0-4.4 0a11.23 11.23 0 0 0-.5-1.02a.1.1 0 0 0-.1-.05a16.14 16.14 0 0 0-3.97 1.23a.1.1 0 0 0-.05.04C1.98 12.02.97 19.07 1.4 23.84a.1.1 0 0 0 .04.07a16.27 16.27 0 0 0 4.9 2.48a.1.1 0 0 0 .11-.03a11.5 11.5 0 0 0 1-1.62a.1.1 0 0 0-.06-.15a10.66 10.66 0 0 1-1.54-.73a.1.1 0 0 1-.01-.17c.1-.07.2-.15.3-.22a.1.1 0 0 1 .11-.02c3.2 1.46 6.66 1.46 9.82 0a.1.1 0 0 1 .11.02c.1.08.2.16.3.22a.1.1 0 0 1-.01.17a10.93 10.93 0 0 1-1.54.73a.1.1 0 0 0-.06.15a11.75 11.75 0 0 0 1 1.62a.1.1 0 0 0 .11.03a16.25 16.25 0 0 0 4.9-2.48a.1.1 0 0 0 .04-.07c.52-5.54-.88-12.55-4.32-19.07a.1.1 0 0 0-.05-.04zM8.02 17.69a2.27 2.27 0 0 1-2.07-2.3a2.27 2.27 0 0 1 2.07-2.3a2.27 2.27 0 0 1 2.07 2.3a2.27 2.27 0 0 1-2.07 2.3zm7.96 0a2.27 2.27 0 0 1-2.07-2.3a2.27 2.27 0 0 1 2.07-2.3a2.27 2.27 0 0 1 2.07 2.3a2.27 2.27 0 0 1-2.07 2.3z" />
            </svg>
            <span>Discord Login</span>
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
