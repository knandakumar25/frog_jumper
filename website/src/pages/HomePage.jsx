import React from 'react';
import Hero from '../components/Hero';
import cyberCityBg from '@assets/images/cyber_city_dusk_background.png';
import './HomePage.css';

// Quick-nav teaser cards pointing to other pages
const TEASERS = [
  { label: 'The Story', href: '/story', icon: '📜', desc: 'Earth, occupied. Four heroes rise.' },
  { label: 'Characters', href: '/characters', icon: '⚔️', desc: 'Choose your warrior.' },
  { label: 'Levels', href: '/levels', icon: '🗺️', desc: '5 missions. Increasing stakes.' },
  { label: 'Features', href: '/features', icon: '✨', desc: '50+ languages. Deep systems.' },
  { label: 'Controls', href: '/controls', icon: '🎮', desc: 'Keyboard & gamepad ready.' },
];

export default function HomePage() {
  return (
    <>
      <Hero bgImage={cyberCityBg} />

      {/* Quick-nav teasers */}
      <section className="home-teasers">
        <div className="container home-teasers__grid">
          {TEASERS.map(t => (
            <a key={t.href} href={t.href} className="home-teaser card" id={`home-nav-${t.label.toLowerCase().replace(/\s+/g,'-')}`}>
              <span className="home-teaser__icon">{t.icon}</span>
              <div>
                <div className="home-teaser__label">{t.label}</div>
                <div className="home-teaser__desc">{t.desc}</div>
              </div>
              <span className="home-teaser__arrow">→</span>
            </a>
          ))}
        </div>
      </section>
    </>
  );
}
