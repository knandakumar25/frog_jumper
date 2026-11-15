import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { gameRequest, confirmationPath } from '../auth/gameHandoff';
import { SignUp } from '../auth/clerkGate';
import { Page, useLocalePath } from '../components/Showcase';
export default function Signup() {
  const { t } = useTranslation();
  const base = useLocalePath();
  const { locale: routeLocale } = useParams();
  const locale = routeLocale || base.slice(1);
  const [handoff] = useState(gameRequest);
  const suffix = handoff ? `/game/${handoff}` : '';
  const target = handoff ? confirmationPath(handoff, locale) : `/onboarding?locale=${encodeURIComponent(locale)}`;
  return <Page title={t('showcase.signup')}><div className="auth-panel" data-handoff-target={handoff ? target : ''}><SignUp routing="path" path={`/${locale}/signup${suffix}`} signInUrl={`/${locale}/login${suffix}`} forceRedirectUrl={target} fallbackRedirectUrl={target} /></div></Page>;
}
