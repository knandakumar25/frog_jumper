import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../translations/LanguageContext';
import apiClient from '../api/client';

const TwoFactor = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, setUser } = useAuth();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);

  const [statusMessage, setStatusMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [code, setCode] = useState('');
  const [isEnabling, setIsEnabling] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isEnabled = user?.is_2fa_enabled;

  const startEnable = async () => {
    setErrorMessage('');
    setStatusMessage('');
    setIsSubmitting(true);
    try {
      await apiClient.post('/auth/2fa/enable/');
      setStatusMessage(t('auth.code_sent', 'Verification code sent to your email. Please enter it below.'));
      setIsEnabling(true);
    } catch (e) {
      console.error(e);
      setErrorMessage(t('auth.code_request_failed', 'Failed to request 2FA verification code.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const verifyEnable = async (e) => {
    e.preventDefault();
    setErrorMessage('');
    setStatusMessage('');
    setIsSubmitting(true);
    try {
      await apiClient.post('/auth/2fa/verify-enable/', { code });
      // Refresh profile to get updated flag
      const profileRes = await apiClient.get('/auth/profile/');
      setUser(profileRes.data);
      setStatusMessage(t('auth.two_factor_enabled_success', 'Two‑Factor Authentication enabled successfully.'));
      setIsEnabling(false);
    } catch (e) {
      console.error(e);
      setErrorMessage(t('auth.invalid_code', 'Invalid code or verification failed.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const disable2FA = async () => {
    setErrorMessage('');
    setStatusMessage('');
    setIsSubmitting(true);
    try {
      await apiClient.patch('/auth/profile/', { is_2fa_enabled: false });
      const profileRes = await apiClient.get('/auth/profile/');
      setUser(profileRes.data);
      setStatusMessage(t('auth.two_factor_disabled_success', 'Two‑Factor Authentication disabled.'));
    } catch (e) {
      console.error(e);
      setErrorMessage(t('auth.disable_failed', 'Failed to disable 2FA.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const goBack = () => {
    navigate(`/${currentLocale}/account`);
  };

  return (
    <div className="pt-[120px] md:pt-[150px] pb-32 px-4 flex justify-center items-center">
      <div className="w-full max-w-md bg-surface-container-lowest border-2 border-primary-container/30 p-8">
        <h2 className="text-headline-lg font-headline-lg text-primary-container uppercase mb-8 text-center font-extrabold tracking-wider">
          {t('auth.two_factor_title', 'Two‑Factor Authentication')}
        </h2>

        {errorMessage && (
          <div className="bg-error/15 border border-error/50 p-4 mb-4 text-error text-sm font-semibold">
            {errorMessage}
          </div>
        )}
        {statusMessage && (
          <div className="bg-primary-container/15 border border-primary-container/50 p-4 mb-4 text-primary-container text-sm font-semibold">
            {statusMessage}
          </div>
        )}

        {isEnabled ? (
          <div className="flex flex-col gap-4 items-center">
            <p className="text-center">{t('auth.two_factor_enabled_desc', '2FA is currently enabled on your account.')}</p>
            <button
              type="button"
              onClick={disable2FA}
              disabled={isSubmitting}
              className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 disabled:opacity-50"
            >
              {isSubmitting ? t('auth.processing', 'Processing…') : t('auth.disable_2fa', 'Disable 2FA')}
            </button>
          </div>
        ) : (
          isEnabling ? (
            <form onSubmit={verifyEnable} className="flex flex-col gap-4">
              <label className="text-xs uppercase font-bold text-on-surface-variant">{t('auth.verification_code', 'Verification Code')}</label>
              <input
                type="text"
                placeholder={t('auth.enter_code', 'Enter code')}
                className="bg-surface-container-highest border-2 border-outline-variant/40 p-3.5 text-on-surface outline-none focus:border-primary-container transition-all text-sm font-body-md"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                required
                disabled={isSubmitting}
              />
              <button
                type="submit"
                disabled={isSubmitting}
                className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 disabled:opacity-50"
              >
                {isSubmitting ? t('auth.verifying', 'Verifying…') : t('auth.verify_enable', 'Verify & Enable')}
              </button>
            </form>
          ) : (
            <button
              type="button"
              onClick={startEnable}
              disabled={isSubmitting}
              className="brutal-btn py-4 font-headline-md text-headline-md uppercase font-bold text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container transition-all duration-150 disabled:opacity-50 w-full"
            >
              {isSubmitting ? t('auth.requesting', 'Requesting…') : t('auth.enable_two_factor', 'Enable Two‑Factor Authentication')}
            </button>
          )
        )}

        <div className="mt-6 text-center">
          <button type="button" onClick={goBack} className="text-primary-container underline hover:text-secondary-container">
            {t('auth.back_to_account', 'Back to My Account')}
          </button>
        </div>
      </div>
    </div>
  );
};

export default TwoFactor;
