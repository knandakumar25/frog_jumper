import React, { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate, useParams, useLocation, useNavigate } from 'react-router-dom'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import HomePage from './pages/HomePage'
import StoryPage from './pages/StoryPage'
import CharactersPage from './pages/CharactersPage'
import LevelsPage from './pages/LevelsPage'
import FeaturesPage from './pages/FeaturesPage'
import ControlsPage from './pages/ControlsPage'
import { LanguageProvider, useLanguage, parseLocaleCode } from './translations/LanguageContext'
import LanguageModal from './translations/LanguageModal'
import './App.css'

function LocaleRoutes({ children }) {
  const { locale } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedLanguage, selectedCountry, setSelectedLanguage, setSelectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();

  useEffect(() => {
    if (!locale) return;
    
    const parsed = parseLocaleCode(locale, LANGUAGE_CONFIG);
    if (!parsed) {
      // Invalid locale segment
      const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
      const rest = location.pathname.startsWith('/') ? location.pathname.slice(1) : location.pathname;
      navigate(`/${currentLocale}/${rest}`, { replace: true });
    } else {
      // Valid locale - update context state if different to sync state
      const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
      if (locale !== currentLocale) {
        setSelectedLanguage(parsed.langId);
        setSelectedCountry(parsed.countryName);
      }
    }
  }, [locale, selectedLanguage, selectedCountry, navigate, location.pathname, LANGUAGE_CONFIG, getLocaleCode, setSelectedLanguage, setSelectedCountry]);

  const parsed = parseLocaleCode(locale, LANGUAGE_CONFIG);
  if (!parsed) {
    return null;
  }

  return children;
}

function LocaleRedirect() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const location = useLocation();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  
  const pathParts = location.pathname.split('/');
  const firstSegment = pathParts[1];
  
  const parsed = parseLocaleCode(firstSegment, LANGUAGE_CONFIG);
  if (parsed) {
    // Already contains a valid locale but didn't match any route
    return <HomePage />;
  }
  
  const rest = location.pathname.startsWith('/') ? location.pathname.slice(1) : location.pathname;
  return <Navigate to={`/${currentLocale}/${rest}`} replace />;
}

function Layout() {
  return (
    <>
      <Navbar />
      <main>
        <Routes>
          <Route path="/:locale" element={<LocaleRoutes><HomePage /></LocaleRoutes>} />
          <Route path="/:locale/story" element={<LocaleRoutes><StoryPage /></LocaleRoutes>} />
          <Route path="/:locale/characters" element={<LocaleRoutes><CharactersPage /></LocaleRoutes>} />
          <Route path="/:locale/levels" element={<LocaleRoutes><LevelsPage /></LocaleRoutes>} />
          <Route path="/:locale/features" element={<LocaleRoutes><FeaturesPage /></LocaleRoutes>} />
          <Route path="/:locale/controls" element={<LocaleRoutes><ControlsPage /></LocaleRoutes>} />
          {/* Fallback */}
          <Route path="*" element={<LocaleRedirect />} />
        </Routes>
      </main>
      <Footer />
      <LanguageModal />
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <LanguageProvider>
        <Layout />
      </LanguageProvider>
    </BrowserRouter>
  )
}

