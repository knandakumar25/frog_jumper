import React, { useEffect, useRef, useState } from 'react';
import './Hero.css';

const TYPEWRITER_LINES = [
  'AXIOM-7 ONLINE',
  'The year is 2157.',
  'Earth has fallen under colonial occupation.',
  'Four champions emerged from the margins.',
  'Join the Homeland Liberation Front.',
  'Reclaim what was taken.',
];

export default function Hero({ bgImage }) {
  const [displayText, setDisplayText] = useState('');
  const [lineIndex, setLineIndex] = useState(0);
  const [charIndex, setCharIndex] = useState(0);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    const currentLine = TYPEWRITER_LINES[lineIndex];

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
          setLineIndex(i => (i + 1) % TYPEWRITER_LINES.length);
        }
      }
    }, isDeleting ? 40 : 70);

    return () => clearTimeout(timeout);
  }, [charIndex, isDeleting, lineIndex]);

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
          <span>AXIOM-7 ONLINE — MISSION ACTIVE</span>
        </div>

        <h1 className="hero__title">
          <span className="hero__title-line1">AXIOM FORCE</span>
          <span className="hero__title-line2">
            RECLAIM <span className="hero__title-accent">EARTH</span>
          </span>
        </h1>

        <div className="hero__typewriter" aria-live="polite">
          <span className="hero__typewriter-prompt">AXIOM-7 &gt;</span>
          <span className="hero__typewriter-text">{displayText}</span>
          <span className="hero__typewriter-cursor" aria-hidden="true">█</span>
        </div>

        <p className="hero__description">
          A 3D action platformer set in a dystopian 2157 Los Angeles.
          Battle through alien-occupied territories, collect cherries, and fight
          for humanity's freedom as one of four elite warriors.
        </p>

        <div className="hero__tags">
          {['3D Platformer', 'Action', 'Sci-Fi', '4 Levels', 'Godot 4.6'].map(tag => (
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
            Play Now
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
            The Story
          </a>
        </div>

        {/* Stats */}
        <div className="hero__stats">
          {[
            { value: '5', label: 'Characters' },
            { value: '4+', label: 'Levels' },
            { value: '50+', label: 'Languages' },
            { value: '∞', label: 'Replayability' },
          ].map(stat => (
            <div key={stat.label} className="hero__stat">
              <span className="hero__stat-value">{stat.value}</span>
              <span className="hero__stat-label">{stat.label}</span>
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
