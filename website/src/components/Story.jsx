import React, { useRef, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import './Story.css';

const STORY_EVENTS = [
  {
    year: '2157',
    titleKey: 'story.timeline.arrival_title',
    textKey: 'story.timeline.arrival_desc',
    icon: '🛸',
    color: 'var(--color-secondary)',
  },
  {
    year: '2157 — Week 1',
    titleKey: 'story.timeline.displacement_title',
    textKey: 'story.timeline.displacement_desc',
    icon: '🏚️',
    color: 'var(--color-accent)',
  },
  {
    year: '2157 — Month 2',
    titleKey: 'story.timeline.resistance_title',
    textKey: 'story.timeline.resistance_desc',
    icon: '✊',
    color: 'var(--color-primary)',
  },
  {
    year: '2157 — Month 3',
    titleKey: 'story.timeline.axiom_title',
    textKey: 'story.timeline.axiom_desc',
    icon: '🤖',
    color: 'var(--color-green)',
  },
  {
    year: 'Now',
    titleKey: 'story.timeline.mission_title',
    textKey: 'story.timeline.mission_desc',
    icon: '⚔️',
    color: 'var(--color-gold)',
  },
];

function useInView(threshold = 0.2) {
  const ref = useRef(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) setInView(true); },
      { threshold }
    );
    if (ref.current) observer.observe(ref.current);
    return () => observer.disconnect();
  }, [threshold]);

  return [ref, inView];
}

export default function Story({ bgImage, hideSectionHeader }) {
  const [sectionRef, inView] = useInView(0.1);
  const { t } = useTranslation();

  return (
    <section className="story section grid-bg" id="story" ref={sectionRef}>
      <div className="container">
        {!hideSectionHeader && (
          <div className="story__header">
            <span className="section-label">// {t('story.briefing')}</span>
            <h2 className="section-title">
              The <span className="text-gradient-primary">{t('story.title')}</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              {t('story.desc')}
            </p>
          </div>
        )}

        <div className="story__layout">
          {/* Image panel */}
          <div className={`story__image-panel ${inView ? 'story__image-panel--visible' : ''}`}>
            <div className="story__image-frame">
              <img
                src={bgImage}
                alt="Dystopian Los Angeles under Xelthara occupation"
                className="story__image"
              />
              <div className="story__image-overlay" />
              <div className="story__image-caption">
                <span className="story__image-caption-label">{t('story.live_feed')}</span>
                <span>{t('story.caption')}</span>
              </div>
            </div>

            {/* AXIOM-7 quote */}
            <div className="story__axiom-quote">
              <div className="story__axiom-header">
                <span className="story__axiom-dot" />
                <span className="story__axiom-id">AXIOM-7</span>
              </div>
              <p className="story__axiom-text">
                {t('story.axiom_text')}
              </p>
            </div>
          </div>

          {/* Timeline */}
          <div className={`story__timeline ${inView ? 'story__timeline--visible' : ''}`}>
            <div className="story__timeline-line" />
            {STORY_EVENTS.map((event, i) => (
              <div
                key={i}
                className="story__event"
                style={{ animationDelay: `${i * 0.15}s` }}
              >
                <div
                  className="story__event-dot"
                  style={{ background: event.color, boxShadow: `0 0 12px ${event.color}` }}
                >
                  <span>{event.icon}</span>
                </div>
                <div className="story__event-content card">
                  <span className="story__event-year" style={{ color: event.color }}>
                    {event.year}
                  </span>
                  <h3 className="story__event-title">{t(event.titleKey)}</h3>
                  <p className="story__event-text">{t(event.textKey)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
