import { useTranslation } from 'react-i18next';
import { useLanguage } from '../translations/LanguageContext';
import './Footer.css';

export default function Footer() {
  const year = new Date().getFullYear();
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t } = useTranslation();

  return (
    <footer className="footer" id="footer">
      {/* CTA Banner */}
      <div className="footer__cta">
        <div className="footer__cta-bg" />
        <div className="container footer__cta-inner">
          <div>
            <h2 className="footer__cta-title">
              {t('story.timeline.mission_title')}
            </h2>
            <p className="footer__cta-sub">
              {t('story.timeline.mission_desc')}
            </p>
          </div>
          <div className="footer__cta-actions">
            <a
              href="https://github.com/knandakumar25/frog_jumper"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-primary footer__cta-btn"
              id="footer-play-btn"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                <path d="M8 5v14l11-7z"/>
              </svg>
              {t('hero.play_on_github')}
            </a>
            <a
              href="https://github.com/knandakumar25/frog_jumper"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-outline footer__cta-btn"
              id="footer-github-btn"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z"/>
              </svg>
              {t('hero.view_source')}
            </a>
          </div>
        </div>
      </div>

      {/* Footer body */}
      <div className="footer__body">
        <div className="container footer__body-inner">
          <div className="footer__brand">
            <div className="footer__logo">
              <span className="footer__logo-icon">⬡</span>
              <span className="footer__logo-text">
                {t('hero.title_line1') === 'AXIOM FORCE' ? (
                  <>AXIOM<span className="footer__logo-accent"> FORCE</span></>
                ) : (
                  t('hero.title_line1')
                )}
              </span>
            </div>
            <p className="footer__brand-desc">
              {t('footer.tagline')}
            </p>
            <div className="footer__axiom-status">
              <span className="footer__axiom-dot" />
              <span className="footer__axiom-text">{t('footer.status')}</span>
            </div>
          </div>

          <div className="footer__links-group">
            <h4 className="footer__links-title">{t('footer.nav_title')}</h4>
            <ul className="footer__links">
              {[
                { labelKey: 'home.story_label', href: '/' },
                { labelKey: 'nav.story', href: '/story' },
                { labelKey: 'nav.characters', href: '/characters' },
                { labelKey: 'nav.levels', href: '/levels' },
                { labelKey: 'nav.features', href: '/features' },
                { labelKey: 'nav.controls', href: '/controls' },
              ].map(link => {
                const targetHref = link.href.startsWith('/') 
                  ? `/${currentLocale}${link.href === '/' ? '' : link.href}` 
                  : link.href;
                return (
                  <li key={link.href}>
                    <a href={targetHref} className="footer__link">
                      {t(link.labelKey)}
                    </a>
                  </li>
                );
              })}
            </ul>
          </div>

          <div className="footer__links-group">
            <h4 className="footer__links-title">{t('footer.tech_title')}</h4>
            <ul className="footer__links">
              {[
                { label: 'Godot Engine', href: 'https://godotengine.org/' },
                { label: '.NET 8 SDK', href: 'https://dotnet.microsoft.com/' },
                { label: 'godot-sqlite', href: 'https://github.com/2shady4u/godot-sqlite' },
                { label: 'Pixel Adventure', href: '#' },
                { label: 'Comic Neue Font', href: 'https://comicneue.com/' },
              ].map(link => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="footer__link"
                    target={link.href.startsWith('http') ? '_blank' : undefined}
                    rel={link.href.startsWith('http') ? 'noopener noreferrer' : undefined}
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div className="footer__links-group">
            <h4 className="footer__links-title">{t('footer.credits_title')}</h4>
            <ul className="footer__links footer__credits">
              <li>Sprites: Pixel Adventure Pack</li>
              <li>Backgrounds: Cyber City Dusk</li>
              <li>Music: MusicGPT</li>
              <li>Font: Comic Neue & Noto Sans</li>
              <li>Engine: Godot 4.6</li>
            </ul>
          </div>
        </div>

        <div className="footer__bottom">
          <div className="container footer__bottom-inner">
            <span className="footer__copyright">
              {t('footer.copyright', { year })}
            </span>
            <span className="footer__mono">
              v1.0.0 — GODOT 4.6 — MIT LICENSE
            </span>
          </div>
        </div>
      </div>
    </footer>
  );
}

