import React, { useRef, useEffect, useState } from 'react';
import './Story.css';

const STORY_EVENTS = [
  {
    year: '2157',
    title: 'The Arrival',
    text: 'The Xelthara Collective arrives on Earth, declaring it "discovered territory" and humanity "an inconvenient indigenous population."',
    icon: '🛸',
    color: 'var(--color-secondary)',
  },
  {
    year: '2157 — Week 1',
    title: 'Mass Displacement',
    text: 'Within days, millions are displaced into cramped containment zones while the Xelthara colonize our cities and dismantle our monuments.',
    icon: '🏚️',
    color: 'var(--color-accent)',
  },
  {
    year: '2157 — Month 2',
    title: 'The Resistance Forms',
    text: 'From the margins of the containment zones, four champions emerge — Ninja Frog, Mask Dude, Pink Man, and Virtual Guy — refusing to be erased.',
    icon: '✊',
    color: 'var(--color-primary)',
  },
  {
    year: '2157 — Month 3',
    title: 'AXIOM-7 Deployed',
    text: 'The Homeland Liberation Front activates AXIOM-7, an Automated External Intelligence Operations Module, to coordinate the resistance.',
    icon: '🤖',
    color: 'var(--color-green)',
  },
  {
    year: 'Now',
    title: 'Your Mission Begins',
    text: 'You have been chosen. Infiltrate colonial territories. Sabotage power cores. Dismantle settlement infrastructure. Reclaim Earth.',
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

  return (
    <section className="story section grid-bg" id="story" ref={sectionRef}>
      <div className="container">
        {!hideSectionHeader && (
          <div className="story__header">
            <span className="section-label">// narrative briefing</span>
            <h2 className="section-title">
              The <span className="text-gradient-primary">Story</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              Earth has fallen under a dystopian alien occupation. A small team of warriors
              must fight their way through enemy territory to reclaim it. Choose your fighter,
              survive the gauntlet, and prove yourself worthy of saving the world.
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
                <span className="story__image-caption-label">LIVE FEED</span>
                <span>Los Angeles — Sector 4 Containment Zone</span>
              </div>
            </div>

            {/* AXIOM-7 quote */}
            <div className="story__axiom-quote">
              <div className="story__axiom-header">
                <span className="story__axiom-dot" />
                <span className="story__axiom-id">AXIOM-7</span>
              </div>
              <p className="story__axiom-text">
                "Infiltrate their colonial territories. Sabotage the power cores
                fueling their terraforming. Every zone you liberate pushes back
                their occupation and reminds them: Earth has inhabitants who
                will fight for it."
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
                  <h3 className="story__event-title">{event.title}</h3>
                  <p className="story__event-text">{event.text}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
