import React, { useState, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSelector from '../translations/LanguageSelector';
import { useLanguage } from '../translations/LanguageContext';
import { useAuth } from '../context/AuthContext';
import AuthModal from './auth/AuthModal';

const NAV_LINKS = [
  { key: 'home', to: '' },
  { key: 'gameplay', to: '/gameplay' },
  { key: 'squad', to: '/squad' },
];

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [isAuthOpen, setIsAuthOpen] = useState(false);
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 50);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  useEffect(() => {
    setMenuOpen(false);
    window.scrollTo({ top: 0 });
  }, [location.pathname]);

  const getNavLabel = (key) => {
    return t(`nav.${key}`, key.toUpperCase().replace('_', ' '));
  };

  const playNowText = t('hero.title_line2_prefix', 'RECLAIM ') + t('hero.title_line2_highlight', 'EARTH');

  return (
    <>
      {/* Desktop and Tablet Navbar */}
      <nav className={`fixed top-0 w-full z-50 transition-all duration-300 ${
        scrolled
          ? 'bg-surface/90 backdrop-blur-xl border-b-2 border-primary-container/30 shadow-[0_4px_20px_rgba(0,243,255,0.15)]'
          : 'bg-surface/80 backdrop-blur-md border-b border-primary-container/10'
      }`}>
        <div className="flex justify-between items-center px-4 md:px-margin-desktop py-4 max-w-container-max-width mx-auto">
          {/* Logo */}
          <NavLink to={`/${currentLocale}`} className="flex items-center gap-2 font-display-hero text-[20px] md:text-headline-md text-primary-container drop-shadow-[0_0_8px_rgba(0,243,255,0.8)] font-bold tracking-tighter uppercase select-none hover:opacity-95">
            <span>⬡</span>
            <span>
              {t('hero.title_line1') === 'AXIOM FORCE' ? (
                <>AXIOM <span className="text-secondary-container drop-shadow-[0_0_8px_rgba(254,0,254,0.8)]">FORCE</span></>
              ) : (
                t('hero.title_line1')
              )}
            </span>
          </NavLink>

          {/* Desktop Links */}
          <div className="hidden lg:flex gap-6 items-center">
            {NAV_LINKS.map(link => (
              <NavLink
                key={link.key}
                to={`/${currentLocale}${link.to}`}
                end={link.to === ''}
                className={({ isActive }) =>
                  `font-headline-md text-[14px] md:text-[16px] uppercase tracking-widest px-3 py-1.5 transition-all duration-150 hover:skew-x-[-12deg] hover:bg-primary-container hover:text-on-primary-container ${
                    isActive
                      ? 'text-primary-container border-b-2 border-primary-container'
                      : 'text-on-surface-variant hover:text-primary'
                  }`
                }
              >
                {getNavLabel(link.key)}
              </NavLink>
            ))}
          </div>

          {/* Desktop Right Side CTA & Lang */}
          <div className="hidden lg:flex items-center gap-4">
            {user ? (
              <div className="flex items-center gap-4">
                <span className="text-xs font-bold text-primary-container uppercase tracking-tighter">
                  {user.username}
                </span>
                <button
                  onClick={logout}
                  className="text-xs uppercase font-bold text-on-surface-variant hover:text-error transition-colors underline"
                >
                  Logout
                </button>
              </div>
            ) : (
              <button
                onClick={() => setIsAuthOpen(true)}
                className="text-xs font-bold uppercase text-on-surface-variant hover:text-primary transition-colors underline"
              >
                Login / Sign Up
              </button>
            )}
            <LanguageSelector />
            <a
              href="https://github.com/knandakumar25/frog_jumper"
              target="_blank"
              rel="noopener noreferrer"
              className="brutal-btn font-label-caps text-label-caps px-5 py-2.5 font-bold uppercase hover:scale-105 transition-all duration-150 active:scale-95"
            >
              {playNowText}
            </a>
          </div>

          {/* Mobile Menu Toggle */}
          <button
            className="lg:hidden text-primary-container p-2 focus:outline-none"
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label="Toggle Navigation Menu"
          >
            <span className="material-symbols-outlined text-[32px]">
              {menuOpen ? 'close' : 'menu'}
            </span>
          </button>
        </div>

        {/* Mobile Navigation Drawer */}
        <div className={`lg:hidden fixed left-0 w-full bg-surface-container-lowest/95 backdrop-blur-2xl border-b border-primary-container/20 transition-all duration-300 ease-in-out overflow-hidden z-40 ${
          menuOpen ? 'max-h-[500px] py-6' : 'max-h-0'
        }`}>
          <div className="flex flex-col gap-4 px-margin-mobile items-center">
            {NAV_LINKS.map(link => (
              <NavLink
                key={link.key}
                to={`/${currentLocale}${link.to}`}
                end={link.to === ''}
                className={({ isActive }) =>
                  `w-full text-center font-headline-md text-[18px] uppercase tracking-wider py-2 transition-all duration-75 ${
                    isActive
                      ? 'text-primary-container bg-primary-container/10 border-r-4 border-l-4 border-primary-container'
                      : 'text-on-surface-variant hover:text-primary hover:bg-surface-container/50'
                  }`
                }
              >
                {getNavLabel(link.key)}
              </NavLink>
            ))}

            <div className="h-[1px] bg-outline-variant/30 w-full my-2"></div>

            <div className="flex flex-col gap-4 items-center w-full">
              <LanguageSelector />
              {user ? (
                <button
                  onClick={logout}
                  className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase"
                >
                  Logout ({user.username})
                </button>
              ) : (
                <button
                  onClick={() => setIsAuthOpen(true)}
                  className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase"
                >
                  Login / Sign Up
                </button>
              )}
              <a
                href="https://github.com/knandakumar25/frog_jumper"
                target="_blank"
                rel="noopener noreferrer"
                className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase"
              >
                {playNowText}
              </a>
            </div>
          </div>
        </div>
      </nav>
      <AuthModal isOpen={isAuthOpen} onClose={() => setIsAuthOpen(false)} />
    </>
  );
}
