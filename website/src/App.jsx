import React, { useEffect, useRef } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageProvider, useLanguage } from './translations/LanguageContext';
import LanguageModal from './translations/LanguageModal';
import Navbar from './components/Navbar';
import Footer from './components/Footer';
import { AuthProvider } from './context/AuthContext';

// Pages
import Home from './pages/Home';
import Gameplay from './pages/Gameplay';
import Squad from './pages/Squad';
import TechHub from './pages/TechHub';
import DesignSystem from './pages/DesignSystem';
import BrowserAuth from './pages/BrowserAuth';
import Onboarding from './pages/Onboarding';
import Login from './pages/Login';
import Signup from './pages/Signup';
import TwoFactor from './pages/TwoFactor';
import Account from './pages/Account';
import DeleteAccount from './pages/DeleteAccount';
import FAQ from './pages/FAQ';
import AccountAccess from './components/AccountAccess';

// Redirect helper to default or stored locale
function LocaleRedirect() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  return <Navigate to={`/${currentLocale}`} replace />;
}

function MainLayout() {
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const previousPath = useRef(location.pathname);
  useEffect(() => {
    const page = location.pathname.includes('/auth/browser-confirm') ? 'signin' : location.pathname === '/onboarding' ? 'onboarding' : location.pathname.endsWith('/account/2fa') ? 'security' : location.pathname.endsWith('/account/delete') ? 'deleteAccount' : location.pathname.split('/')[2] || 'home';
    const titleKey = ({ 'tech-hub': 'development', login: 'login', signup: 'signup', account: 'account', signin: 'signinQ' })[page] || page;
    const title = page === 'design-system' ? t('nav.design_system') : page === 'deleteAccount' ? t('account.delete_title') : t(`showcase.${titleKey}`, t('showcase.account'));
    document.title = `${title} | AXIOM FORCE`;
    if (previousPath.current !== location.pathname) {
      document.getElementById('main-content')?.focus();
      window.scrollTo(0, 0);
      previousPath.current = location.pathname;
    }
  }, [location.pathname, i18n.language, t]);
  return (
    <div className="site-shell">
      <Navbar />
      <main id="main-content" tabIndex={-1}>
        <Routes>
          <Route path="/" element={<LocaleRedirect />} />
          <Route path="/:locale" element={<Home />} />
          <Route path="/:locale/gameplay" element={<Gameplay />} />
          <Route path="/:locale/squad" element={<Squad />} />
          <Route path="/:locale/tech-hub" element={<TechHub />} />
          <Route path="/:locale/design-system" element={<DesignSystem />} />
          <Route path="/:locale/faq" element={<FAQ />} />
          <Route path="/:locale/login/*" element={<Login />} />
          <Route path="/:locale/signup/*" element={<Signup />} />
          <Route path="/:locale/account" element={<AccountAccess><Account /></AccountAccess>} />
          <Route path="/:locale/account/2fa" element={<AccountAccess><TwoFactor /></AccountAccess>} />
          {/* Account deletion; also the web deletion address for the Play listing (2026-11-18). */}
          <Route path="/:locale/account/delete" element={<AccountAccess><DeleteAccount /></AccountAccess>} />
          <Route path="/auth/browser-confirm" element={<BrowserAuth />} />
          {/*
            Registered BARE, not under /:locale. LocaleRedirect matches the "*"
            catch-all and does <Navigate to={`/${locale}`} replace />, which
            discards window.location.search -- so a locale-prefixed onboarding
            URL would silently drop ?token= and render a blank form with no
            error. (The same trap is already swallowing the ?access= query on
            the OAuth /auth-callback redirects.)
          */}
          <Route path="/onboarding" element={<Onboarding />} />
          <Route path="*" element={<LocaleRedirect />} />
        </Routes>
      </main>
      <Footer />
      <LanguageModal />
    </div>
  );
}

export default function App() {
  return (
    <Router>
      <AuthProvider>
        <LanguageProvider>
          <MainLayout />
        </LanguageProvider>
      </AuthProvider>
    </Router>
  );
}
