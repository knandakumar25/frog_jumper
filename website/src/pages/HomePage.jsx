import React from 'react';
import { useTranslation } from 'react-i18next';
import Hero from '../components/Hero';
import cyberCityBg from '@assets/images/cyber_city_dusk_background.png';
import { useLanguage } from '../translations/LanguageContext';
import './HomePage.css';

// Quick-nav teaser cards pointing to other pages
const TEASERS = [
  { labelKey: 'home.story_label', descKey: 'home.story_desc', href: '/story', icon: '📜', testId: 'home-nav-the-story' },
  { labelKey: 'home.char_label', descKey: 'home.char_desc', href: '/characters', icon: '⚔️', testId: 'home-nav-characters' },
  { labelKey: 'home.levels_label', descKey: 'home.levels_desc', href: '/levels', icon: '🗺️', testId: 'home-nav-levels' },
  { labelKey: 'home.features_label', descKey: 'home.features_desc', href: '/features', icon: '✨', testId: 'home-nav-features' },
  { labelKey: 'home.controls_label', descKey: 'home.controls_desc', href: '/controls', icon: '🎮', testId: 'home-nav-controls' },
];

export default function HomePage() {
  const { selectedLanguage, selectedCountry, LANGUAGE_CONFIG, getLocaleCode } = useLanguage();
  const currentLocale = getLocaleCode(selectedLanguage, selectedCountry, LANGUAGE_CONFIG);
  const { t, i18n } = useTranslation();
  const isRtl = i18n.dir() === 'rtl';

  return (
    <>
      <Hero bgImage={cyberCityBg} />

      {/* Quick-nav teasers */}
      <section className="home-teasers">
        <div className="container home-teasers__grid">
          {TEASERS.map(teaser => {
            const targetHref = `/${currentLocale}${teaser.href}`;
            return (
              <a key={teaser.href} href={targetHref} className="home-teaser card" id={teaser.testId}>
                <span className="home-teaser__icon">{teaser.icon}</span>
                <div>
                  <div className="home-teaser__label">{t(teaser.labelKey)}</div>
                  <div className="home-teaser__desc">{t(teaser.descKey)}</div>
                </div>
                <span className="home-teaser__arrow">{isRtl ? '←' : '→'}</span>
              </a>
            );
          })}
        </div>
      </section>
    </>
  );
}

