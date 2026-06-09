import React from 'react';
import './Features.css';

const FEATURES = [
  {
    icon: '👥',
    title: '5 Playable Characters',
    desc: 'Four main warriors plus Cyber Shogun — an unlockable bonus character with unique sword and karate attack combos.',
    tag: 'ROSTER',
    color: 'var(--color-primary)',
  },
  {
    icon: '📜',
    title: 'Deep Story Narrative',
    desc: 'A typewriter-style story intro powered by the Ink runtime — an explicit narrative about resistance against colonial occupation.',
    tag: 'NARRATIVE',
    color: 'var(--color-gold)',
  },
  {
    icon: '🍒',
    title: 'Collectibles & Scoring',
    desc: 'Cherries scattered across all levels worth points. Scores are saved locally via SQLite and displayed on a persistent leaderboard.',
    tag: 'PROGRESSION',
    color: 'var(--color-accent)',
  },
  {
    icon: '💾',
    title: 'Session System',
    desc: 'Create multiple save sessions and continue exactly where you left off. Your progress persists across playthroughs.',
    tag: 'SAVE SYSTEM',
    color: 'var(--color-green)',
  },
  {
    icon: '🌍',
    title: '50+ Language Support',
    desc: 'Fully localised UI including Arabic, Hindi, Japanese, Malayalam, and many more — powered by CSV-based translation.',
    tag: 'LOCALISATION',
    color: 'var(--color-secondary)',
  },
  {
    icon: '🎮',
    title: 'Gamepad Support',
    desc: 'Full controller support including D-Pad, analogue stick movement, and button mapping for all attacks and abilities.',
    tag: 'CONTROLS',
    color: 'var(--color-primary)',
  },
  {
    icon: '⏸️',
    title: 'Blur Pause Menu',
    desc: 'A premium pause menu with real-time blur shader effect — adjustable music and SFX volumes accessible at any time.',
    tag: 'UX',
    color: 'var(--color-gold)',
  },
  {
    icon: '🎵',
    title: 'Original Soundtrack',
    desc: '"Dystopian Dawn" — original music generated with MusicGPT to perfectly match the dystopian sci-fi atmosphere.',
    tag: 'AUDIO',
    color: 'var(--color-green)',
  },
];

const TECH = [
  { name: 'Godot 4.6', desc: 'Game Engine' },
  { name: 'C# / .NET 8', desc: 'Game Logic' },
  { name: 'GDScript', desc: 'Scene Scripting' },
  { name: 'SQLite', desc: 'Score Persistence' },
  { name: 'Ink Runtime', desc: 'Narrative Engine' },
  { name: 'MSBuild', desc: 'Build System' },
];

export default function Features({ hideSectionHeader }) {
  return (
    <section className="features section" id="features">
      <div className="container">
        {!hideSectionHeader && (
          <div className="features__header">
            <span className="section-label">// system capabilities</span>
            <h2 className="section-title">
              Game <span className="text-gradient-primary">Features</span>
            </h2>
            <div className="divider" />
          </div>
        )}

        <div className="features__grid">
          {FEATURES.map((feat, i) => (
            <div
              key={feat.title}
              className="features__card card"
              style={{ '--feat-color': feat.color }}
            >
              <div className="features__card-top">
                <span className="features__icon">{feat.icon}</span>
                <span
                  className="features__tag"
                  style={{ color: feat.color, borderColor: `${feat.color}40` }}
                >
                  {feat.tag}
                </span>
              </div>
              <h3 className="features__title">{feat.title}</h3>
              <p className="features__desc">{feat.desc}</p>
            </div>
          ))}
        </div>

        {/* Tech Stack */}
        <div className="features__tech">
          <h3 className="features__tech-title">
            <span className="section-label" style={{ fontSize: '0.7rem' }}>// built with</span>
            Tech Stack
          </h3>
          <div className="features__tech-grid">
            {TECH.map(t => (
              <div key={t.name} className="features__tech-item">
                <div className="features__tech-dot" />
                <div>
                  <div className="features__tech-name">{t.name}</div>
                  <div className="features__tech-desc">{t.desc}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
