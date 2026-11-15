import { useEffect, useRef, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { SignedIn, SignedOut, UserButton } from '../auth/clerkGate';
import { gameRequest } from '../auth/gameHandoff';
import LanguageSelector from '../translations/LanguageSelector';
import { useLanguage } from '../translations/LanguageContext';
import TitleLogo from './TitleLogo';
import { useLocalePath } from './Showcase';

export default function Navbar() {
  const { t } = useTranslation();
  const { selectedLanguage } = useLanguage();
  const base = useLocalePath();
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const toggle = useRef(null);
  const sid = gameRequest();
  const suffix = sid ? `/game/${sid}` : '';
  useEffect(() => {
    setOpen(wasOpen => { if (wasOpen) toggle.current?.focus(); return false; });
  }, [location.pathname, location.search]);
  useEffect(() => {
    const close = event => { if (event.key === 'Escape' && open) { setOpen(false); toggle.current?.focus(); } };
    document.addEventListener('keydown', close);
    return () => document.removeEventListener('keydown', close);
  }, [open]);
  const links = <>{['home', 'gameplay', 'squad', 'faq'].map(key => <NavLink key={key} end={key === 'home'} to={`${base}${key === 'home' ? '' : `/${key}`}`}>{t(`showcase.${key}`)}</NavLink>)}</>;
  const account = <><SignedIn><NavLink to={`${base}/account`}>{t('showcase.account')}</NavLink><UserButton afterSignOutUrl={base} /></SignedIn><SignedOut><NavLink to={`${base}/login${suffix}`}>{t('showcase.login')}</NavLink><NavLink className="button secondary" to={`${base}/signup${suffix}`}>{t('showcase.signup')}</NavLink></SignedOut></>;
  return <header className="site-header"><a className="skip-link" href="#main-content">{t('showcase.skip')}</a><nav aria-label={t('showcase.menu')}>
    <NavLink className="brand" to={base}><TitleLogo lang={selectedLanguage} className="nav-logo" /></NavLink>
    <div className="desktop-nav">{links}</div><div className="desktop-account">{account}<LanguageSelector /></div>
    <button className="menu-toggle" ref={toggle} aria-expanded={open} aria-controls="mobile-menu" onClick={() => setOpen(!open)}>{t(open ? 'showcase.close' : 'showcase.menu')}</button>
    <div id="mobile-menu" className="mobile-menu" hidden={!open}>{links}{account}<LanguageSelector /></div>
  </nav></header>;
}
