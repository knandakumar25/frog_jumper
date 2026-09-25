import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSelector from '../translations/LanguageSelector';
import { useLanguage } from '../translations/LanguageContext';
import { getTitleLogoPath } from '../translations/titleLogo';
import { useAuth } from '../context/AuthContext';

const NAV_LINKS = [
  { key: 'home', to: '' },
  { key: 'gameplay', to: '/gameplay' },
  { key: 'squad', to: '/squad' },
  { key: 'faq', to: '/faq' },
];

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const profileRef = useRef(null);
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

  useEffect(() => {
    function handleClickOutside(event) {
      if (profileRef.current && !profileRef.current.contains(event.target)) {
        setProfileOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

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
          <NavLink to={`/${currentLocale}`} className="flex items-center gap-2 select-none hover:opacity-95 transition-opacity">
            <img 
              src={getTitleLogoPath(selectedLanguage)} 
              alt="AXIOM FORCE" 
              className="h-10 md:h-12 w-auto object-contain drop-shadow-[0_0_15px_rgba(0,243,255,0.75)] select-none transition-all duration-300"
            />
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
              <div className="relative" ref={profileRef}>
                <div
                  className="flex items-center gap-3 cursor-pointer p-1 hover:bg-primary-container/10 transition-colors"
                  onClick={() => setProfileOpen(!profileOpen)}
                >
                  {user.profile_picture ? (
                    <img
                      src={user.profile_picture}
                      alt="User Avatar"
                      className="w-8 h-8 rounded-none border-2 border-primary-container object-cover shadow-[0_0_10px_rgba(0,243,255,0.3)]"
                    />
                  ) : (
                    <div className="w-8 h-8 rounded-none border-2 border-primary-container/45 flex items-center justify-center bg-surface-container-highest text-primary-container font-bold font-mono text-xs shadow-[0_0_5px_rgba(0,243,255,0.1)]">
                      {user.username.substring(0, 2).toUpperCase()}
                    </div>
                  )}
                  <span className="text-xs font-bold text-primary-container uppercase tracking-tighter">
                    {user.username}
                  </span>
                </div>

                {profileOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-surface-container-highest border-2 border-primary-container shadow-[4px_4px_0px_rgba(0,243,255,0.5)] z-50">
                    <div className="flex flex-col p-2 gap-1">
                      <NavLink
                        to={`/${currentLocale}/account`}
                        onClick={() => setProfileOpen(false)}
                        className="text-xs font-bold uppercase text-on-surface-variant hover:text-primary hover:bg-primary-container/10 px-3 py-2 transition-colors"
                      >
                        {t('nav.my_account', 'My Account')}
                      </NavLink>
                      <NavLink
                        to={`/${currentLocale}/account/2fa`}
                        onClick={() => setProfileOpen(false)}
                        className="text-xs font-bold uppercase text-on-surface-variant hover:text-primary hover:bg-primary-container/10 px-3 py-2 transition-colors"
                      >
                        {t('nav.two_factor_auth', 'Two-Factor Auth')}
                      </NavLink>
                      <div className="h-[1px] bg-outline-variant/30 my-1"></div>
                      <button
                        onClick={logout}
                        className="text-left text-xs font-bold uppercase text-on-surface-variant hover:text-error px-3 py-2 transition-colors"
                      >
                        {t('nav.logout', 'Logout')}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div className="flex items-center gap-4">
                <NavLink
                  to={`/${currentLocale}/login`}
                  className="text-xs font-bold uppercase text-on-surface-variant hover:text-primary transition-colors underline"
                >
                  {t('nav.login', 'Login')}
                </NavLink>
                <NavLink
                  to={`/${currentLocale}/signup`}
                  className="text-xs font-bold uppercase text-primary-container bg-primary-container/10 border-2 border-primary-container/45 hover:bg-primary-container/20 px-3 py-1.5 transition-all duration-150 active:scale-95 font-body-md"
                >
                  {t('nav.signup', 'Sign Up')}
                </NavLink>
              </div>
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
                <div className="flex flex-col gap-3 items-center w-full">
                  <div className="flex items-center gap-3">
                    {user.profile_picture ? (
                      <img
                        src={user.profile_picture}
                        alt="User Avatar"
                        className="w-10 h-10 rounded-none border-2 border-primary-container object-cover shadow-[0_0_10px_rgba(0,243,255,0.3)]"
                      />
                    ) : (
                      <div className="w-10 h-10 rounded-none border-2 border-primary-container/45 flex items-center justify-center bg-surface-container-highest text-primary-container font-bold font-mono text-sm">
                        {user.username.substring(0, 2).toUpperCase()}
                      </div>
                    )}
                    <span className="text-sm font-bold text-primary-container uppercase tracking-wider">
                      {user.username}
                    </span>
                  </div>
                  <button
                    onClick={logout}
                    className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase"
                  >
                    {t('nav.logout', 'Logout')}
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-3 w-full">
                  <NavLink
                    to={`/${currentLocale}/login`}
                    onClick={() => setMenuOpen(false)}
                    className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase"
                  >
                    {t('nav.login', 'Login')}
                  </NavLink>
                  <NavLink
                    to={`/${currentLocale}/signup`}
                    onClick={() => setMenuOpen(false)}
                    className="brutal-btn w-full text-center font-label-caps text-label-caps py-3 font-bold uppercase bg-primary-container text-void-black hover:bg-secondary-container hover:text-on-secondary-container"
                  >
                    {t('nav.signup', 'Sign Up')}
                  </NavLink>
                </div>
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
    </>
  );
}
