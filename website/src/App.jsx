import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { LanguageProvider, useLanguage } from './translations/LanguageContext';
import LanguageModal from './translations/LanguageModal';
import Navbar from './components/Navbar';
import Footer from './components/Footer';

// Pages
import Home from './pages/Home';
import Gameplay from './pages/Gameplay';
import Squad from './pages/Squad';
import TechHub from './pages/TechHub';
import DesignSystem from './pages/DesignSystem';

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
      <LanguageProvider>
        <MainLayout />
      </LanguageProvider>
    </Router>
  );
}
