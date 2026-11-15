import React, { useState, useEffect } from 'react';
import { Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import apiClient from '../api/client';

// Phase 3: the terminal room behind the account panels (code-split).

const getCallingCode = (c) => {
  if (!c.idd || !c.idd.root) return '';
  const suffixes = c.idd.suffixes || [];
  if (suffixes.length === 1) {
    return `${c.idd.root}${suffixes[0]}`;
  }
  return c.idd.root;
};

const Account = () => {
  const { t } = useTranslation();
  const { user, setUser } = useAuth();
  const [formData, setFormData] = useState({
    first_name: '',
    last_name: '',
    email: '',
    phone_number: '',
    country: '',
    country_code: '',
  });
  const [countries, setCountries] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState(null);
  // Changing the email needs a code mailed to the current address (backend change, 2026-11-15).
  const [emailCode, setEmailCode] = useState('');
  const [awaitingEmailCode, setAwaitingEmailCode] = useState(false);

  useEffect(() => {
    const fetchCountriesAndInitialize = async () => {
      let loadedCountries = [];
      try {
        const res = await apiClient.get('/auth/countries/');
        const data = res.data;
        loadedCountries = data.sort((a, b) => a.name.common.localeCompare(b.name.common));
        setCountries(loadedCountries);
      } catch (e) {
        console.error('Failed to fetch countries', e);
      }

      if (user) {
        let phoneNum = user.phone_number || '';
        let code = '';

        if (phoneNum.startsWith('+') && loadedCountries.length > 0) {
          let bestMatch = null;
          let longestMatchLength = 0;

          loadedCountries.forEach(c => {
            const callingCode = getCallingCode(c);
            if (callingCode && phoneNum.startsWith(callingCode)) {
              if (callingCode.length > longestMatchLength) {
                longestMatchLength = callingCode.length;
                bestMatch = callingCode;
              }
            }
          });

          if (bestMatch) {
            code = bestMatch;
            phoneNum = phoneNum.slice(bestMatch.length).trim();
          }
        }

        setFormData({
          first_name: user.first_name || '',
          last_name: user.last_name || '',
          email: user.email || '',
          phone_number: phoneNum,
          country: user.country || '',
          country_code: code,
        });
      }
    };

    fetchCountriesAndInitialize();
  }, [user]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setMessage(null);
    try {
      const emailChanged = formData.email.trim() !== (user.email || '');
      if (emailChanged && !awaitingEmailCode) {
        // First save with a new address: mail a code to the current one and ask for it.
        await apiClient.post('/auth/profile/email-code/');
        setAwaitingEmailCode(true);
        setMessage({ type: 'success', text: t('account.email_code_sent', 'We sent a code to your current email. Enter it below and save again.') });
        return;
      }
      const combinedPhoneNumber = (formData.country_code + formData.phone_number.trim()).trim();
      const payload = {
        first_name: formData.first_name,
        last_name: formData.last_name,
        email: formData.email,
        phone_number: combinedPhoneNumber,
        country: formData.country,
      };
      if (emailChanged) payload.email_code = emailCode.trim();
      const response = await apiClient.patch('/auth/profile/', payload);
      setUser(response.data);
      setAwaitingEmailCode(false);
      setEmailCode('');
      setMessage({ type: 'success', text: 'Profile updated successfully!' });
    } catch (error) {
      console.error('Failed to update profile', error);
      setMessage({ type: 'error', text: 'Failed to update profile.' });
    } finally {
      setIsLoading(false);
    }
  };


  // Deliberately /onboarding, not /login. A user who signed up through Clerk
  // holds a Clerk session but has no Django row until onboarding completes, and
  // sending them to /login just signs them in with Clerk again and lands them
  // back here -- a loop with no exit. /onboarding either finishes the job or
  // tells them plainly that there is no session.
  if (!user) return <Navigate to="/onboarding" replace />;

  return (
    <div className="relative overflow-hidden min-h-screen py-20 px-4 md:px-margin-desktop">
      {/* Phase 3: the terminal room behind the account panels */}
      <div className="absolute inset-0 bg-[radial-gradient(110%_80%_at_50%_10%,#0a2230_0%,#06121b_50%,#050505_100%)]"></div>
      <div className="absolute inset-0 opacity-30 bg-[linear-gradient(rgba(0,243,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(0,243,255,0.05)_1px,transparent_1px)] bg-[size:44px_44px]"></div>

      <div className="relative z-10 max-w-2xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <span className="w-2 h-2 bg-primary-container"></span>
          <span className="font-mono text-[11px] text-primary-container uppercase tracking-[0.25em]">
            // OPERATIVE DOSSIER & NEURAL PROFILE
          </span>
          <div className="h-px bg-primary-container/20 flex-grow"></div>
        </div>

        <h1 className="font-display-hero text-2xl md:text-3xl text-primary-container font-extrabold uppercase tracking-wide mb-6 text-glow-cyan">
          {t('nav.my_account', 'My Account')}
        </h1>

        <form onSubmit={handleSubmit} className="bg-surface-container-low/90 backdrop-blur-md border border-primary-container/30 p-6 md:p-8 relative">
          {/* Accent corners */}
          <div className="absolute top-0 left-0 w-3 h-3 border-t-2 border-l-2 border-primary-container"></div>
          <div className="absolute top-0 right-0 w-3 h-3 border-t-2 border-r-2 border-primary-container"></div>
          <div className="absolute bottom-0 left-0 w-3 h-3 border-b-2 border-l-2 border-primary-container"></div>
          <div className="absolute bottom-0 right-0 w-3 h-3 border-b-2 border-r-2 border-primary-container"></div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div className="flex flex-col gap-1.5">
              <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('auth.first_name', 'First Name')}</label>
              <input
                name="first_name"
                value={formData.first_name}
                onChange={handleInputChange}
                className="bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('auth.last_name', 'Last Name')}</label>
              <input
                name="last_name"
                value={formData.last_name}
                onChange={handleInputChange}
                className="bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
              />
            </div>
            <div className="flex flex-col gap-1.5 md:col-span-2">
              <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('auth.email', 'Email')}</label>
              <input
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                className="bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
              />
            </div>

            {awaitingEmailCode && (
              <div className="flex flex-col gap-1.5 md:col-span-2">
                <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('auth.verification_code', 'Verification Code')}</label>
                <input
                  name="email_code"
                  value={emailCode}
                  onChange={(e) => setEmailCode(e.target.value)}
                  required
                  className="bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
                />
              </div>
            )}

            <div className="flex flex-col gap-1.5 md:col-span-2">
              <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('account.phone_number', 'Phone Number')}</label>
              <div className="flex gap-2">
                <div className="relative w-1/3">
                  <select
                    className="w-full bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[11px]"
                    value={formData.country_code}
                    onChange={(e) => setFormData(prev => ({...prev, country_code: e.target.value}))}
                  >
                    <option value="">{t('account.code', 'Code')}</option>
                    {countries.map(c => {
                      const callingCode = getCallingCode(c);
                      if (!callingCode) return null;
                      return (
                        <option key={c.cca3} value={callingCode}>
                          {c.flag ? c.flag + ' ' : ''}{c.name.common} ({callingCode})
                        </option>
                      );
                    })}
                  </select>
                </div>
                <input
                  name="phone_number"
                  placeholder="123 456 7890"
                  value={formData.phone_number}
                  onChange={handleInputChange}
                  className="flex-grow bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
                />
              </div>
            </div>

            <div className="flex flex-col gap-1.5 md:col-span-2">
              <label className="text-[11px] font-mono font-bold uppercase tracking-wider text-on-surface-variant">{t('account.country', 'Country')}</label>
              <div className="flex items-center gap-2">
                <select
                  name="country"
                  value={formData.country}
                  onChange={handleInputChange}
                  className="flex-grow bg-void-black/80 border border-outline-variant/40 p-2.5 text-on-surface focus:border-primary-container outline-none transition-colors font-mono text-[13px]"
                >
                  <option value="">{t('auth.select_country', 'Select Country')}</option>
                  {countries.map(c => (
                    <option key={c.cca3} value={c.name.common}>
                      {c.flag ? c.flag + ' ' : ''}{c.name.common}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          {message && (
            <div className={`mt-6 p-3 text-xs font-mono font-bold uppercase border ${message.type === 'success' ? 'border-tertiary-container/50 bg-tertiary-container/10 text-tertiary-container' : 'border-error bg-error/10 text-error'}`}>
              &gt; {message.text}
            </div>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="mt-8 cyber-cut w-full bg-primary-container text-void-black font-headline-md text-[14px] uppercase py-3.5 px-6 font-bold tracking-widest hover:bg-secondary-container hover:text-white transition-all shadow-[0_0_15px_rgba(0,243,255,0.3)] disabled:opacity-50"
          >
            {isLoading ? t('account.saving', 'Saving...') : t('account.save_changes', 'Save Changes')}
          </button>
        </form>
      </div>
    </div>
  );
};

export default Account;
