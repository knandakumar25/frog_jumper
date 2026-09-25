import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient, { BACKEND_URL } from '../api/client';

export default function Signup() {
  const { t } = useTranslation();
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [profilePicture, setProfilePicture] = useState('');
  const [previewUrl, setPreviewUrl] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [country, setCountry] = useState('');
  const [countries, setCountries] = useState([]);
  const [isLoadingCountries, setIsLoadingCountries] = useState(true);
  const [countryError, setCountryError] = useState(false);

  useEffect(() => {
    let isMounted = true;

    const fetchUNCountries = async () => {
      try {
        const cached = sessionStorage.getItem('un_countries_list_v2');
        if (cached) {
          const parsed = JSON.parse(cached);
          if (Array.isArray(parsed) && parsed.length > 0) {
            if (isMounted) {
              setCountries(parsed);
              setIsLoadingCountries(false);
            }
            return;
          }
        }

        const res = await fetch('https://unstats.un.org/SDGAPI/v1/sdg/GeoArea/Tree');
        if (!res.ok) throw new Error(`UN SDG API HTTP ${res.status}`);
        const data = await res.json();

        const countryMap = new Map();
        const walk = (nodes) => {
          if (!Array.isArray(nodes)) return;
          for (const node of nodes) {
            if (node.type === 'Country' && node.geoAreaName) {
              const rawName = node.geoAreaName.trim();
              // Index Palestine as 'Palestine (State of)' so it sorts naturally under 'P'
              const displayName = rawName === 'State of Palestine' ? 'Palestine (State of)' : rawName;
              if (displayName && !countryMap.has(displayName)) {
                countryMap.set(displayName, {
                  name: displayName,
                  value: rawName,
                  code: String(node.geoAreaCode || '')
                });
              }
            }
            if (node.children && node.children.length > 0) {
              walk(node.children);
            }
          }
        };
        walk(data);

        const sorted = Array.from(countryMap.values()).sort((a, b) =>
          a.name.localeCompare(b.name)
        );

        if (isMounted) {
          setCountries(sorted);
          setIsLoadingCountries(false);
          sessionStorage.setItem('un_countries_list_v2', JSON.stringify(sorted));
        }
      } catch (err) {
        console.error('Failed to load countries from UN API:', err);
        if (isMounted) {
          setCountryError(true);
          setIsLoadingCountries(false);
        }
      }
    };

    fetchUNCountries();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleFileChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      setErrorMsg('Image size must be less than 2MB');
      return;
    }

    const reader = new FileReader();
    reader.onloadend = () => {
      setProfilePicture(reader.result);
      setPreviewUrl(reader.result);
    };
    reader.readAsDataURL(file);
  };

  const handleRemovePhoto = () => {
    setProfilePicture('');
    setPreviewUrl('');
  };

  const { register, login, setUser } = useAuth();
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

    if (!country) {
      setErrorMsg('Please select your country of residence.');
      return;
    }

    setIsSubmitting(true);

    try {
      // 1. Create account
      await register(username, email, password, firstName, lastName, profilePicture, country);
      // 2. Perform auto-login for a seamless experience
      await login(username, password);
      
      if (redirectTarget) {
        navigate(redirectTarget);
      } else {
        navigate(`/${currentLocale}`);
      }
    } catch (err) {
      console.error(err);
      setErrorMsg('Registration failed. Username or email may already be registered.');
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
          {t('auth.create_account', 'CREATE ACCOUNT')}
        </h2>

        {errorMsg && (
          <div className="bg-error/15 border border-error/50 p-4 mb-6 text-error text-sm font-semibold flex items-center gap-2">
            <span className="material-symbols-outlined text-[20px]">warning</span>
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          {/* Profile Picture Upload */}
          <div className="flex flex-col items-center gap-3 mb-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant text-center w-full">
              {t('auth.avatar_label', 'Avatar Port / Profile Picture (Optional)')}
            </label>
            <div className="relative group">
              <div className="w-24 h-24 rounded-none border-2 border-dashed border-outline-variant/60 hover:border-primary-container/80 transition-all flex items-center justify-center bg-surface-container-highest cursor-pointer relative overflow-hidden group shadow-[0_0_15px_rgba(0,243,255,0.05)] hover:shadow-[0_0_20px_rgba(0,243,255,0.2)]">
                {previewUrl ? (
                  <img
                    src={previewUrl}
                    alt="Avatar Preview"
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <div className="flex flex-col items-center justify-center text-on-surface-variant group-hover:text-primary-container transition-colors">
                    <span className="material-symbols-outlined text-[32px]">add_a_photo</span>
                    <span className="text-[9px] uppercase font-bold mt-1 tracking-tighter">{t('auth.upload_portrait', 'Upload Portrait')}</span>
                  </div>
                )}
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleFileChange}
                  className="absolute inset-0 opacity-0 cursor-pointer"
                  disabled={isSubmitting}
                />
              </div>
              {previewUrl && (
                <button
                  type="button"
                  onClick={handleRemovePhoto}
                  className="absolute -top-2 -right-2 bg-error text-void-black rounded-none p-1 flex items-center justify-center hover:bg-error-container hover:text-on-error-container transition-colors shadow-lg"
                  title="Remove Photo"
                  disabled={isSubmitting}
                >
                  <span className="material-symbols-outlined text-[16px]">close</span>
                </button>
              )}
            </div>
          </div>

          {/* First Name & Last Name (Optional) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant flex justify-between">
                <span>{t('auth.first_name', 'First Name')}</span>
                <span className="text-[10px] lowercase italic opacity-60">{t('auth.optional', 'optional')}</span>
              </label>
              <input
                type="text"
                placeholder={t('auth.placeholder_first_name', 'First name')}
                className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant flex justify-between">
                <span>{t('auth.last_name', 'Last Name')}</span>
                <span className="text-[10px] lowercase italic opacity-60">{t('auth.optional', 'optional')}</span>
              </label>
              <input
                type="text"
                placeholder={t('auth.placeholder_last_name', 'Last name')}
                className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              {t('auth.username', 'Username')}
            </label>
            <input
              type="text"
              placeholder={t('auth.enter_username', 'Enter your username')}
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              {t('auth.email_address', 'Email Address')}
            </label>
            <input
              type="email"
              placeholder={t('auth.enter_email', 'Enter your email')}
              className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              {t('auth.country_territory', 'Country / Territory')}
            </label>
            <div className="relative">
              <select
                className="w-full bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md cursor-pointer appearance-none pr-10"
                value={country}
                onChange={(e) => setCountry(e.target.value)}
                required
                disabled={isSubmitting || isLoadingCountries}
              >
                <option value="" disabled className="bg-surface-container-highest text-on-surface-variant">
                  {isLoadingCountries ? t('auth.loading_countries', 'Loading official UN country database...') : t('auth.select_country', 'Select your country')}
                </option>
                {countries.map((c) => (
                  <option key={c.code || c.name} value={c.name} className="bg-surface text-on-surface py-1">
                    {c.name}
                  </option>
                ))}
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-on-surface-variant">
                <span className="material-symbols-outlined text-[20px]">arrow_drop_down</span>
              </div>
            </div>
            {countryError && (
              <span className="text-[11px] text-error font-medium">
                {t('auth.country_error', 'Unable to load UN country list. Please check your connection or refresh.')}
              </span>
            )}
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-xs uppercase font-bold tracking-wider text-on-surface-variant">
              {t('auth.password', 'Password')}
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
            className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 hover:skew-x-[-8deg] active:scale-95 disabled:opacity-50 mt-3"
          >
            {isSubmitting ? t('auth.registering', 'Registering...') : t('auth.initialize_profile', 'Initialize Profile')}
          </button>
        </form>

        <div className="flex items-center gap-3 my-5">
          <div className="h-[1px] bg-outline-variant/30 flex-grow"></div>
          <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant/60">{t('auth.or', 'OR')}</span>
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
            <span>{t('auth.epic_login', 'Epic Games Login')}</span>
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
            <span>{t('auth.steam_login', 'Steam Login')}</span>
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
            <span>{t('auth.discord_login', 'Discord Login')}</span>
          </button>
        </div>

        <div className="mt-8 text-center text-xs font-body-md border-t border-primary-container/10 pt-6">
          <span className="text-on-surface-variant">{t('auth.already_have_account', 'Already have an account? ')}</span>
          <Link
            to={`/${currentLocale}/login${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`}
            className="text-primary-container hover:text-secondary-container font-bold underline transition-colors"
          >
            {t('auth.login_access_terminal', 'Login / Access Terminal')}
          </Link>
        </div>
      </div>
    </div>
  );
}
