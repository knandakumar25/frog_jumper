import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
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
import FAQ from './pages/FAQ';

// Redirect helper to default or stored locale
function LocaleRedirect() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  return <Navigate to={`/${currentLocale}`} replace />;
}

function MainLayout() {
  return (
    <div className="flex flex-col min-h-screen bg-surface text-on-surface relative antialiased crt-flicker">
      {/* Global scanline overlay */}
      <div className="fixed inset-0 glitch-overlay pointer-events-none z-[9999] opacity-75"></div>
      <Navbar />
      <main className="flex-grow">
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
          <Route path="/:locale/account" element={<Account />} />
          <Route path="/:locale/account/2fa" element={<TwoFactor />} />
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
