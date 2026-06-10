import React, { useState, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSelector from '../translations/LanguageSelector';
import { useLanguage } from '../translations/LanguageContext';
import './Navbar.css';

const NAV_LINKS = [
  { key: 'story', to: '/story' },
  { key: 'characters', to: '/characters' },
  { key: 'levels', to: '/levels' },
  { key: 'features', to: '/features' },
  { key: 'controls', to: '/controls' },
];

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 50);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  // Close menu on route change
  useEffect(() => {
    setMenuOpen(false);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, [location.pathname]);

  return (
    <nav className={`navbar ${scrolled ? 'navbar--scrolled' : ''}`} id="navbar">
      <div className="navbar__inner container">
        <NavLink to={`/${currentLocale}`} className="navbar__logo">
          <span className="navbar__logo-icon">⬡</span>
          <span className="navbar__logo-text">
            {t('hero.title_line1') === 'AXIOM FORCE' ? (
              <>AXIOM<span className="navbar__logo-accent"> FORCE</span></>
            ) : (
              t('hero.title_line1')
            )}
          </span>
        </NavLink>

        <ul className={`navbar__links ${menuOpen ? 'navbar__links--open' : ''}`}>
          {NAV_LINKS.map(link => (
            <li key={link.to}>
              <NavLink
                to={`/${currentLocale}${link.to}`}
                className={({ isActive }) =>
                  `navbar__link${isActive ? ' navbar__link--active' : ''}`
                }
              >
                {t(`nav.${link.key}`)}
              </NavLink>
            </li>
          ))}

          <li className="navbar__cta-container">
            <a
              href="https://github.com/knandakumar25/frog_jumper"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-primary navbar__cta"
            >
              {t('nav.play_now')}
            </a>
            <LanguageSelector />
          </li>
        </ul>

        <button
          className={`navbar__burger ${menuOpen ? 'navbar__burger--open' : ''}`}
          onClick={() => setMenuOpen(o => !o)}
          aria-label="Toggle navigation"
          id="nav-menu-toggle"
        >
          <span /><span /><span />
        </button>
      </div>
    </nav>
  );
}

