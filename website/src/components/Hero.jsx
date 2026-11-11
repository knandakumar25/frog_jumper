import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import TitleLogo from './TitleLogo';
import './Hero.css';

const TYPEWRITER_KEYS = [
  'footer.status',
  'story.timeline.arrival_title',
  'story.timeline.displacement_title',
  'story.timeline.resistance_title',
  'story.timeline.axiom_title',
  'story.timeline.mission_title'
];

export default function Hero({ bgImage }) {
  const [displayText, setDisplayText] = useState('');
  const [lineIndex, setLineIndex] = useState(0);
  const [charIndex, setCharIndex] = useState(0);
  const [isDeleting, setIsDeleting] = useState(false);
  const { t, i18n } = useTranslation();

  useEffect(() => {
    const currentLine = t(TYPEWRITER_KEYS[lineIndex]);

    const timeout = setTimeout(() => {
      if (!isDeleting) {
        if (charIndex < currentLine.length) {
          setDisplayText(currentLine.slice(0, charIndex + 1));
          setCharIndex(c => c + 1);
        } else {
          setTimeout(() => setIsDeleting(true), 2000);
        }
      } else {
        if (charIndex > 0) {
          setDisplayText(currentLine.slice(0, charIndex - 1));
          setCharIndex(c => c - 1);
        } else {
          setIsDeleting(false);
          setLineIndex(i => (i + 1) % TYPEWRITER_KEYS.length);
        }
      }
    }, isDeleting ? 40 : 70);

    return () => clearTimeout(timeout);
  }, [charIndex, isDeleting, lineIndex, t]);

  const handleScrollDown = () => {
    document.querySelector('#story')?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <section className="hero scanlines" id="hero">
      {/* Background */}
      <div className="hero__bg">
        <img
          src={bgImage}
          alt="Cyber City Dusk - Axiom Force world"
          className="hero__bg-img"
        />
        <div className="hero__bg-gradient" />
        <div className="hero__bg-vignette" />
      </div>

      {/* Particle overlay */}
      <div className="hero__particles" aria-hidden="true">
        {[...Array(20)].map((_, i) => (
          <div
            key={i}
            className="hero__particle"
            style={{
              left: `${Math.random() * 100}%`,
              top: `${Math.random() * 100}%`,
              animationDelay: `${Math.random() * 4}s`,
              animationDuration: `${3 + Math.random() * 4}s`,
              width: `${1 + Math.random() * 2}px`,
              height: `${1 + Math.random() * 2}px`,
            }}
          />
        ))}
      </div>

      {/* Content */}
      <div className="hero__content container">
        <div className="hero__badge">
          <span className="hero__badge-dot" />
          <span>{t('footer.status')}</span>
        </div>

        <div className="hero__title-container flex justify-center my-4">
          <h1 className="sr-only">{t('hero.title_line1')} {t('hero.title_line2_prefix')}{t('hero.title_line2_highlight')}</h1>
          <TitleLogo
            lang={i18n.language}
            alt="AXIOM FORCE Logo"
            className="w-full max-w-[680px] h-auto object-contain drop-shadow-[0_0_35px_rgba(0,243,255,0.9)] select-none transition-all duration-300"
            textClassName="text-4xl md:text-6xl"
            fetchPriority="high"
          />
        </div>

        <div className="hero__typewriter" aria-live="polite">
          <span className="hero__typewriter-prompt">AXIOM-7 &gt;</span>
          <span className="hero__typewriter-text">{displayText}</span>
          <span className="hero__typewriter-cursor" aria-hidden="true">█</span>
        </div>

        <p className="hero__description">
          {t('hero.subtitle')}
        </p>

        <div className="hero__tags">
          {['2D Action Platformer', '9 Target Platforms', '6 Operatives', '10 Story Levels', 'Godot 4.7.1 C#'].map(tag => (
            <span key={tag} className="hero__tag">{tag}</span>
          ))}
        </div>

        <div className="hero__actions">
          <a
            href="https://github.com/knandakumar25/frog_jumper"
            target="_blank"
            rel="noopener noreferrer"
            className="btn btn-primary hero__btn"
            id="hero-play-btn"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
              <path d="M8 5v14l11-7z"/>
            </svg>
            {t('nav.play_now')}
          </a>
          <a
            href="#story"
            className="btn btn-outline hero__btn"
            id="hero-story-btn"
            onClick={e => { e.preventDefault(); handleScrollDown(); }}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
            {t('home.story_label')}
          </a>
        </div>

        {/* Stats */}
        <div className="hero__stats">
          {[
            { value: '9', labelKey: 'hero.platforms_stat', defaultLabel: 'Target Platforms' },
            { value: '6', labelKey: 'home.char_label', defaultLabel: 'Operatives' },
            { value: '10', labelKey: 'home.levels_label', defaultLabel: 'Story Levels' },
            { value: '17', labelKey: 'modal.language', defaultLabel: 'Languages' },
          ].map(stat => (
            <div key={stat.labelKey} className="hero__stat">
              <span className="hero__stat-value">{stat.value}</span>
              <span className="hero__stat-label">{t(stat.labelKey, stat.defaultLabel)}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Scroll indicator */}
      <button className="hero__scroll-indicator" onClick={handleScrollDown} id="hero-scroll-down" aria-label="Scroll down">
        <span className="hero__scroll-arrow" />
      </button>
    </section>
  );
}
