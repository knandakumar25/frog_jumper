import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
// Via the gate so the navbar degrades to signed-out links (not a crash) when
// the publishable key is absent from a build. See src/auth/clerkGate.jsx.
import { SignedIn, SignedOut, UserButton } from '../auth/clerkGate.jsx';
import LanguageSelector from '../translations/LanguageSelector';
import { useLanguage } from '../translations/LanguageContext';
import { getTitleLogoPath } from '../translations/titleLogo';

const NAV_LINKS = [
  { key: 'home', to: '' },
  { key: 'gameplay', to: '/gameplay' },
  { key: 'squad', to: '/squad' },
  { key: 'tech_hub', to: '/tech-hub' },
  { key: 'faq', to: '/faq' },
];

// Auth runs on Clerk's hosted Account Portal rather than in-app routes, so the
// Login / Sign Up controls link off-site.
//
// The publishable key (pk_test_<base64 of the FRONTEND API host>$) encodes only the
// Frontend API host, which is NOT where the hosted pages live. On a dev instance the
// two are siblings:
//     Frontend API : <slug>.clerk.accounts.dev
//     Account Portal: <slug>.accounts.dev      <- where /sign-in and /sign-up resolve
// So we rewrite the host only when it matches that dev shape. A production instance
// puts its portal on the app's own domain, which we can't infer from the key, so we
// deliberately return null and let the caller fall back to the in-app route rather
// than construct a link that 404s.
const ACCOUNT_PORTAL_BASE = (() => {
  const encoded = (import.meta.env.VITE_CLERK_PUBLISHABLE_KEY ?? '').split('_')[2];
  if (!encoded) return null;
  try {
    const apiHost = atob(encoded).replace(/\$$/, '');
    if (!apiHost) return null;
    const portalHost = apiHost.replace(/\.clerk\.accounts\.dev$/, '.accounts.dev');
    if (portalHost === apiHost) return null;
    return `https://${portalHost}`;
  } catch {
    return null;
  }
})();

// A live status ticker under the command rail: the diegetic touch that makes
// the shell read as an operations console rather than a website header.
function RailTicker() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  const clock = now.toTimeString().slice(0, 8);
  return (
    <div className="border-b border-primary-container/15 bg-primary-container/[0.05]">
      <div className="max-w-container-max-width mx-auto px-4 md:px-margin-desktop h-6 flex items-center gap-3 overflow-hidden whitespace-nowrap font-mono text-[10px] tracking-[0.18em] text-primary-container/70">
        <span className="text-tertiary-container">&#9679; AXIOM-7 LINK ESTABLISHED</span>
        <span className="opacity-30">|</span>
        <span>RESISTANCE ACTIVE</span>
        <span className="opacity-30">|</span>
        <span className="hidden sm:inline">SECTOR 04 // LA QZ</span>
        <span className="hidden md:inline opacity-30">|</span>
        <span className="hidden md:inline">GRID INTEGRITY 87%</span>
        <span className="ml-auto text-primary-container/80">{clock} LOCAL</span>
      </div>
    </div>
  );
}

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const profileRef = useRef(null);
  const location = useLocation();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();

  // Always resolve to something clickable. If the portal host can't be derived we
  // fall back to the in-app route, which still renders the themed Clerk component,
  // rather than emitting an href-less link that silently does nothing.
  //
  // The portal is off-site, so it cannot know where to send the user afterwards
  // unless we say so in the query string. A bare portal link makes Clerk fall back
  // to the instance home URL and show its "Development mode. You are signed in, but
  // Clerk cannot redirect to your application" page -- which is exactly what a plain
  // <slug>.accounts.dev/sign-up did. `redirect_url` is the documented fix, and it
  // must be ABSOLUTE: a relative path would resolve against <slug>.accounts.dev.
  //
  // Sign-up lands on the bare /onboarding route -- never /:locale/onboarding, since
  // the "*" catch-all in App.jsx rewrites to /:locale and discards the query string.
  // Sign-in lands on /:locale/account, which itself bounces anyone who never finished
  // onboarding over to /onboarding, so both kinds of sign-in end up in the right place.
  const portalUrl = (portalPath, appPath) =>
    ACCOUNT_PORTAL_BASE
      ? `${ACCOUNT_PORTAL_BASE}${portalPath}?redirect_url=${encodeURIComponent(`${window.location.origin}${appPath}`)}`
      : null;

  const portalSignInUrl = portalUrl('/sign-in', `/${currentLocale}/account`) ?? `/${currentLocale}/login`;
  const portalSignUpUrl = portalUrl('/sign-up', '/onboarding') ?? `/${currentLocale}/signup`;

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
        // Profile is now handled by Clerk's UserButton
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const getNavLabel = (key) => {
    return t(`nav.${key}`, key.toUpperCase().replace('_', ' '));
  };

  return (
    <>
      {/* Command rail: the site's shell as an HLF operations console */}
      <nav className={`fixed top-0 w-full z-50 transition-all duration-300 ${
        scrolled ? 'shadow-[0_4px_24px_rgba(0,243,255,0.12)]' : ''
      }`}>
        <div className="border-b border-primary-container/25 bg-void-black/85 backdrop-blur-xl">
          <div className="flex items-center gap-4 px-4 md:px-margin-desktop h-14 max-w-container-max-width mx-auto">
            {/* identity block */}
            <NavLink to={`/${currentLocale}`} className="flex items-center gap-3 select-none group">
              <span className="w-2 h-2 bg-tertiary-container shadow-[0_0_8px_#36fd0f] animate-pulse"></span>
              <img
                src={getTitleLogoPath(selectedLanguage)}
                alt="AXIOM FORCE"
                className="h-7 md:h-8 w-auto object-contain drop-shadow-[0_0_12px_rgba(0,243,255,0.7)] group-hover:drop-shadow-[0_0_18px_rgba(0,243,255,0.95)] transition-all duration-300"
              />
              <span className="hidden md:inline font-mono text-[10px] tracking-[0.3em] text-on-surface-variant/60">//&nbsp;HLF&nbsp;OPS</span>
            </NavLink>

            {/* command tokens */}
            <div className="hidden lg:flex items-center gap-1 mx-auto">
              {NAV_LINKS.map(link => (
                <NavLink
                  key={link.key}
                  to={`/${currentLocale}${link.to}`}
                  end={link.to === ''}
                  className={({ isActive }) =>
                    `font-mono text-[12px] tracking-[0.18em] px-3 py-1.5 border transition-all duration-150 ${
                      isActive
                        ? 'text-primary-container border-primary-container/60 bg-primary-container/10'
                        : 'text-on-surface-variant border-transparent hover:text-primary hover:border-primary-container/30 hover:bg-primary-container/5'
                    }`
                  }
                >
                  <span className="opacity-50">&gt;&nbsp;</span>{getNavLabel(link.key)}
                </NavLink>
              ))}
            </div>

            {/* session block */}
            <div className="hidden lg:flex items-center gap-3 ml-auto">
              <SignedIn>
                <UserButton afterSignOutUrl={`/${currentLocale}`} />
              </SignedIn>
              <SignedOut>
                <a
                  href={portalSignInUrl}
                  className="font-mono text-[11px] tracking-[0.18em] uppercase text-on-surface-variant hover:text-primary transition-colors"
                >
                  {t('nav.login', 'Login')}
                </a>
                <a
                  href={portalSignUpUrl}
                  className="font-mono text-[11px] tracking-[0.18em] uppercase text-void-black bg-primary-container hover:bg-secondary-container hover:text-on-secondary-container px-3 py-1.5 transition-all duration-150"
                >
                  {t('nav.signup', 'Sign Up')}
                </a>
              </SignedOut>
              <LanguageSelector />
            </div>

            {/* mobile toggle */}
            <button
              className="lg:hidden text-primary-container p-2 ml-auto focus:outline-none"
              onClick={() => setMenuOpen(!menuOpen)}
              aria-label="Toggle Navigation Menu"
            >
              <span className="material-symbols-outlined text-[28px]">
                {menuOpen ? 'close' : 'menu'}
              </span>
            </button>
          </div>
        </div>

        <RailTicker />

        {/* mobile drawer: same rail language */}
        <div className={`lg:hidden fixed left-0 w-full bg-void-black/95 backdrop-blur-2xl border-b border-primary-container/20 transition-all duration-300 ease-in-out overflow-hidden z-40 ${
          menuOpen ? 'max-h-[520px] py-5' : 'max-h-0'
        }`}>
          <div className="flex flex-col gap-1 px-margin-mobile">
            {NAV_LINKS.map(link => (
              <NavLink
                key={link.key}
                to={`/${currentLocale}${link.to}`}
                end={link.to === ''}
                className={({ isActive }) =>
                  `w-full font-mono text-[13px] tracking-[0.2em] uppercase py-3 px-3 border-l-2 transition-all duration-75 ${
                    isActive
                      ? 'text-primary-container border-primary-container bg-primary-container/10'
                      : 'text-on-surface-variant border-transparent hover:text-primary hover:border-primary-container/40'
                  }`
                }
              >
                <span className="opacity-50">&gt;&nbsp;</span>{getNavLabel(link.key)}
              </NavLink>
            ))}
            <div className="h-[1px] bg-outline-variant/30 w-full my-3"></div>
            <div className="flex flex-col gap-3">
              <LanguageSelector />
              <SignedIn>
                <UserButton afterSignOutUrl={`/${currentLocale}`} />
              </SignedIn>
              <SignedOut>
                <a
                  href={portalSignInUrl}
                  onClick={() => setMenuOpen(false)}
                  className="w-full text-center font-mono text-[12px] tracking-[0.2em] uppercase py-3 border border-primary-container/40 text-primary-container"
                >
                  {t('nav.login', 'Login')}
                </a>
                <a
                  href={portalSignUpUrl}
                  onClick={() => setMenuOpen(false)}
                  className="w-full text-center font-mono text-[12px] tracking-[0.2em] uppercase py-3 bg-primary-container text-void-black font-bold"
                >
                  {t('nav.signup', 'Sign Up')}
                </a>
              </SignedOut>
            </div>
          </div>
        </div>
      </nav>
    </>
  );
}
