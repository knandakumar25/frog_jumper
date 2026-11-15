import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Link } from 'react-router-dom';

// Phase 3: the terminal room behind the onboarding panels (code-split).
// Via the gate: without a publishable key the stub returns a signed-out,
// loaded state, so start mode never POSTs and token mode (no Clerk session
// needed) still works. See src/auth/clerkGate.jsx.
import { useUser, useClerkSession } from '../auth/clerkGate.jsx';
import { gameRequest, confirmationPath } from '../auth/gameHandoff';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

// Local, not UTC: a user east of UTC submitting on their birthday would be
// rejected by the backend's future-date check otherwise.
const todayIso = (() => {
  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
})();

// DRF returns {field: ["message"]}; surface the first one.
const firstFieldError = (data) => {
  if (!data || typeof data !== 'object') return null;
  for (const value of Object.values(data)) {
    if (Array.isArray(value) && value.length) return String(value[0]);
    if (typeof value === 'string') return value;
  }
  return null;
};

function Panel({ children }) {
  return (
    <div className="relative overflow-hidden pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="absolute inset-0 bg-[radial-gradient(110%_80%_at_50%_15%,#0a2230_0%,#06121b_50%,#050505_100%)]"></div>
      <div className="absolute inset-0 opacity-40 bg-[linear-gradient(rgba(0,243,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(0,243,255,0.05)_1px,transparent_1px)] bg-[size:44px_44px]"></div>

      <div className="relative z-10 w-full max-w-md bg-surface-container-lowest/95 border-2 border-primary-container/30 p-8 shadow-[0_0_30px_rgba(0,243,255,0.15)] bracket-corners">
        <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-primary-container"></div>
        <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-primary-container"></div>
        <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-primary-container"></div>
        {children}
      </div>
    </div>
  );
}

function Heading({ children }) {
  return (
    <h1 className="text-center text-primary-container font-bold uppercase tracking-widest text-lg mb-6">
      {children}
    </h1>
  );
}

const Onboarding = () => {
  const { t } = useTranslation();
  const { getToken } = useClerkSession();
  const handoff = gameRequest();
  const { loginWithTokens } = useAuth();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode, setSelectedLanguage, parseLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  const { user: clerkUser, isLoaded: clerkLoaded } = useUser();

  // Read the token once, then strip it from the address bar so it stops leaking
  // into history and Referer headers.
  const [token] = useState(
    () => new URLSearchParams(window.location.search).get('token') || ''
  );
  const isTokenMode = Boolean(token);

  // --- token mode ---------------------------------------------------------
  const [tokenPhase, setTokenPhase] = useState('loading'); // loading|form|invalid|expired|done
  const [profile, setProfile] = useState(null);
  const [countries, setCountries] = useState([]);
  const [form, setForm] = useState({ country: '', date_of_birth: '' });
  const [submitError, setSubmitError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // --- start mode ---------------------------------------------------------
  const [startPhase, setStartPhase] = useState('idle'); // idle|sending|sent|error
  const [startInfo, setStartInfo] = useState(null);
  const [startError, setStartError] = useState(null);
  const startedRef = useRef(false);

  useEffect(() => {
    if (token) window.history.replaceState({}, '', '/onboarding');
  }, [token]);

  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    const validate = async () => {
      try {
        const res = await apiClient.get('/auth/onboarding/validate-token/', {
          params: { token },
        });
        if (cancelled) return;
        setProfile(res.data);
        // The emailed token resolves the persisted request even in a fresh tab
        // with no Clerk cookies or original locale query.
        const savedLocale = parseLocaleCode(res.data.browser_locale);
        if (savedLocale) setSelectedLanguage(savedLocale.langId);
        setTokenPhase('form');
      } catch (err) {
        if (cancelled) return;
        const statusCode = err.response?.status;
        if (statusCode === 410) setTokenPhase('expired');
        else if (statusCode === 409) setTokenPhase('done');
        else setTokenPhase('invalid');
      }
    };

    const loadCountries = async () => {
      try {
        const res = await apiClient.get('/auth/countries/basic/');
        if (cancelled) return;
        setCountries(
          [...res.data].sort((a, b) => a.name.common.localeCompare(b.name.common))
        );
      } catch {
        if (!cancelled) setCountries([]);
      }
    };

    validate();
    loadCountries();
    return () => {
      cancelled = true;
    };
  }, [token, parseLocaleCode, setSelectedLanguage]);

  const startOnboarding = useCallback(async () => {
    setStartPhase('sending');
    setStartError(null);
    try {
      const res = await apiClient.post('/auth/onboarding/start/', {
        clerk_user_id: clerkUser.id,
        ...(handoff ? {session_id: handoff, clerk_token: await getToken()} : {}),
      });
      setStartInfo(res.data);
      setStartPhase('sent');
    } catch (err) {
      const statusCode = err.response?.status;
      if (statusCode === 409) {
        setStartError(t('onboarding.already_registered', 'This account is already set up. Please sign in.'));
      } else if (statusCode === 429) {
        setStartError(t('onboarding.rate_limited', 'Too many attempts. Please wait a moment and try again.'));
      } else if (statusCode === 403) {
        setStartError(t('onboarding.unverified', 'That email address is not verified yet. Verify it, then try again.'));
      } else if (statusCode === 503) {
        setStartError(t('onboarding.not_configured', 'Onboarding is not configured on the server.'));
      } else if (statusCode === 502) {
        setStartError(t('onboarding.send_failed', 'We could not send the email. Please try again shortly.'));
      } else if (statusCode === 404) {
        setStartError(t('onboarding.unknown_user', 'We could not find your registration. Please sign up again.'));
      } else {
        setStartError(t('onboarding.generic_error', 'Something went wrong. Please try again.'));
      }
      setStartPhase('error');
    }
  }, [clerkUser, t, handoff, getToken]);

  useEffect(() => {
    if (isTokenMode || !clerkLoaded || !clerkUser) return;
    // StrictMode invokes effects twice in development. Without this guard the
    // second pass would send a second email and rotate the token again.
    if (startedRef.current) return;
    startedRef.current = true;
    startOnboarding();
  }, [isTokenMode, clerkLoaded, clerkUser, startOnboarding]);

  const handleResend = (e) => {
    e.preventDefault();
    if (startPhase === 'sending') return;
    startOnboarding();
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSubmitError(null);
    setIsSubmitting(true);
    try {
      const res = await apiClient.post('/auth/onboarding/complete/', {
        token,
        country: form.country,
        date_of_birth: form.date_of_birth,
      });
      const { access, refresh, user: userData } = res.data;
      if (!loginWithTokens(access, refresh, userData)) throw new Error('Session changed during setup');
      // A full navigation is deliberate. AuthProvider's loadUser effect runs
      // once on mount with [] deps, so updating tokens without the auth context does
      // not update `user` -- a same-document navigate() would leave Account.jsx
      // seeing null and bouncing straight back to /login.
      if (res.data.browser_session_id) {
        // Keep the verified onboarding session in memory across this route change.
        window.history.pushState({}, '', confirmationPath(res.data.browser_session_id, res.data.browser_locale || currentLocale));
        window.dispatchEvent(new PopStateEvent('popstate'));
      } else window.location.assign(`/${currentLocale}`);
    } catch (err) {
      const statusCode = err.response?.status;
      if (statusCode === 410) {
        setTokenPhase('expired');
      } else if (statusCode === 409) {
        setTokenPhase('done');
      } else if (statusCode === 404) {
        setTokenPhase('invalid');
      } else if (statusCode === 400) {
        setSubmitError(
          firstFieldError(err.response?.data) ||
            t('onboarding.generic_error', 'Something went wrong. Please try again.')
        );
      } else {
        setSubmitError(t('onboarding.generic_error', 'Something went wrong. Please try again.'));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // ------------------------------------------------------------------------
  // Token mode render
  // ------------------------------------------------------------------------
  if (isTokenMode) {
    if (tokenPhase === 'loading') {
      return (
        <Panel>
          <Heading>{t('onboarding.verifying', 'Verifying Link')}</Heading>
          <p className="text-center text-on-surface-variant">
            {t('onboarding.verifying_desc', 'Validating your registration link...')}
          </p>
        </Panel>
      );
    }

    if (tokenPhase === 'invalid' || tokenPhase === 'expired' || tokenPhase === 'done') {
      const messages = {
        invalid: [
          t('onboarding.invalid_link', 'Invalid Link'),
          t('onboarding.invalid_link_desc', 'This registration link is not valid. Please sign up again to get a new one.'),
        ],
        expired: [
          t('onboarding.expired_link', 'Link Expired'),
          t('onboarding.expired_link_desc', 'This registration link has expired. Sign up again to receive a fresh one.'),
        ],
        done: [
          t('onboarding.already_done', 'Account Already Set Up'),
          // Covers both 409s /complete/ can return: a replayed link, and an
          // address that already belongs to an account.
          t('onboarding.already_done_desc', 'This account is already set up. You can sign in instead.'),
        ],
      }[tokenPhase];

      return (
        <Panel>
          <Heading>{messages[0]}</Heading>
          <p className="text-center text-on-surface-variant text-sm mb-8">{messages[1]}</p>
          <div className="flex justify-center gap-4">
            <Link
              to={`/${currentLocale}/login`}
              className="brutal-btn py-3 px-6 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95"
            >
              {t('auth.login_access_terminal', 'Login / Access Terminal')}
            </Link>
            <Link
              to={`/${currentLocale}/signup`}
              className="brutal-btn py-3 px-6 font-bold uppercase text-on-surface-variant bg-surface-container-highest hover:bg-surface-container-high transition-all duration-150 active:scale-95"
            >
              {t('auth.sign_up', 'Sign Up')}
            </Link>
          </div>
        </Panel>
      );
    }

    return (
      <Panel>
        <Heading>{t('onboarding.finish_setup', 'Finish Setup')}</Heading>
        <p className="text-center text-on-surface-variant text-sm mb-8">
          {t('onboarding.finish_setup_desc', 'Two required fields remain before your account is created.')}
        </p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <label htmlFor="onboarding-email" className="text-xs font-bold uppercase text-on-surface-variant">
              {t('auth.email', 'Email')}
            </label>
            <input
              id="onboarding-email"
              value={profile?.email || ''}
              readOnly
              className="bg-surface-container-highest border-2 border-outline-variant p-2 font-mono opacity-70 cursor-not-allowed"
            />
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="onboarding-country" className="text-xs font-bold uppercase text-on-surface-variant">
              {t('account.country', 'Country')}
            </label>
            <select
              id="onboarding-country" name="country"
              value={form.country}
              onChange={handleChange}
              required
              className="bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
            >
              <option value="">{t('auth.select_country', 'Select Country')}</option>
              {countries.map((c) => (
                <option key={c.cca3} value={c.name.common}>
                  {c.flag ? c.flag + ' ' : ''}{c.name.common}
                </option>
              ))}
            </select>
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="onboarding-birth" className="text-xs font-bold uppercase text-on-surface-variant">
              {t('onboarding.date_of_birth', 'Date of Birth')}
            </label>
            <input
              id="onboarding-birth" type="date"
              name="date_of_birth"
              value={form.date_of_birth}
              onChange={handleChange}
              required
              min="1900-01-01"
              max={todayIso}
              className="bg-surface-container-lowest border-2 border-outline-variant p-2 text-on-surface focus:border-primary-container outline-none transition-colors font-mono"
            />
          </div>

          {submitError && (
            <div role="alert" className="p-3 text-xs font-bold uppercase border-2 border-error text-error">
              {submitError}
            </div>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="brutal-btn w-full py-3 font-bold uppercase transition-all duration-150 active:scale-95 disabled:opacity-50"
          >
            {isSubmitting
              ? t('account.saving', 'Saving...')
              : t('onboarding.create_account', 'Create Account')}
          </button>
        </form>
      </Panel>
    );
  }

  // ------------------------------------------------------------------------
  // Start mode render
  // ------------------------------------------------------------------------
  if (!clerkLoaded) {
    return (
      <Panel>
        <Heading>{t('onboarding.preparing', 'Preparing')}</Heading>
        <p className="text-center text-on-surface-variant">
          {t('onboarding.loading', 'Loading...')}
        </p>
      </Panel>
    );
  }

  if (!clerkUser) {
    return (
      <Panel>
        <Heading>{t('onboarding.no_session', 'No Session')}</Heading>
        <p className="text-center text-on-surface-variant text-sm mb-8">
          {t('onboarding.no_session_desc', 'Sign up first, and we will email you a link to finish setting up your account.')}
        </p>
        <div className="flex justify-center">
          <Link
            to={`/${currentLocale}/signup`}
            className="brutal-btn py-3 px-6 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95"
          >
            {t('auth.sign_up', 'Sign Up')}
          </Link>
        </div>
      </Panel>
    );
  }

  return (
    <Panel>
      <Heading>{t('onboarding.check_email', 'Check Your Email')}</Heading>

      {startPhase === 'sending' && (
        <p className="text-center text-on-surface-variant text-sm">
          {t('onboarding.sending', 'Sending your registration link...')}
        </p>
      )}

      {startPhase === 'sent' && (
        <div className="flex flex-col gap-4">
          <p className="text-center text-on-surface-variant text-sm leading-relaxed">
            {t('onboarding.check_email_desc', 'We sent a link to finish setting up your account to:')}
          </p>
          <p className="text-center font-mono text-primary-container">{startInfo?.email}</p>
          <p className="text-center text-on-surface-variant text-xs leading-relaxed">
            {t('onboarding.link_note', 'Open it to pick your country and date of birth. Your account is not created until you do.')}
          </p>
          <button
            onClick={handleResend}
            className="brutal-btn py-2.5 px-6 font-bold uppercase text-on-surface-variant bg-surface-container-highest hover:bg-surface-container-high transition-all duration-150 active:scale-95 self-center"
          >
            {t('onboarding.resend', 'Resend Link')}
          </button>
          <p className="text-center text-on-surface-variant text-xs opacity-70">
            {t('onboarding.resend_note', 'Resending replaces the previous link.')}
          </p>
        </div>
      )}

      {startPhase === 'error' && (
        <div className="flex flex-col gap-6">
          <div className="p-3 text-xs font-bold uppercase border-2 border-error text-error text-center">
            {startError}
          </div>
          <button
            onClick={handleResend}
            className="brutal-btn py-2.5 px-6 font-bold uppercase text-primary-container bg-primary-container/10 hover:bg-primary-container/20 transition-all duration-150 active:scale-95 self-center"
          >
            {t('auth.try_again', 'Try Again')}
          </button>
        </div>
      )}
    </Panel>
  );
};

export default Onboarding;
