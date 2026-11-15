import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { gameRequest, confirmationPath } from '../auth/gameHandoff';
import { SignIn } from '../auth/clerkGate';
import { Page, useLocalePath } from '../components/Showcase';
export default function Login() {
  const { t } = useTranslation();
  const base = useLocalePath();
  const { locale: routeLocale } = useParams();
  const locale = routeLocale || base.slice(1);
  const [handoff] = useState(gameRequest);
  const suffix = handoff ? `/game/${handoff}` : '';
  const target = handoff ? confirmationPath(handoff, locale) : null;
  return <Page title={t('showcase.login')}><div className="auth-panel" data-handoff-target={target || ''}>
    {new URLSearchParams(window.location.search).has('expired') && <p role="alert">{t('showcase.sessionExpired')}</p>}
    <SignIn routing="path" path={`/${locale}/login${suffix}`} signUpUrl={`/${locale}/signup${suffix}`} forceRedirectUrl={target || undefined} fallbackRedirectUrl={target || `/${locale}/account`} />
  </div></Page>;
}
