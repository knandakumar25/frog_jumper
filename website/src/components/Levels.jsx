import React, { useState } from 'react';
import './Levels.css';

const LEVELS = [
  {
    id: 0,
    number: '00',
    name: 'Protocol Zero',
    subtitle: 'The Virtual Forge',
    type: 'TRAINING',
    typeColor: 'var(--color-green)',
    description:
      'A vast wireframe simulation of a crumbled downtown Los Angeles skyline. Master your combat chassis in the Neural-Sync Virtual Reality before the real war begins.',
    objectives: ['Complete chassis calibration', 'Destroy simulated Xelthara units', 'Pass the synchronized strike test'],
    threat: 'MINIMAL',
    difficulty: 1,
    bgClass: 'level-bg--dojo',
  },
  {
    id: 1,
    number: '01',
    name: 'Ghost in the Gridlock',
    subtitle: 'Cultural Erasure Protocol',
    type: 'INFILTRATION',
    typeColor: 'var(--color-primary)',
    description:
      'The ruins of the Imperial Palace Gardens. Xelthara terraforming beams dissolve ancient stone lanterns. Retrieve the Data-Blade — a digital repository of forbidden history — before it is erased forever.',
    objectives: ['Engage Demolition Crew drones', 'Neutralize the Demolition Beacon', 'Secure the Data-Blade'],
    threat: 'HIGH',
    difficulty: 2,
    bgClass: 'level-bg--cyber',
  },
  {
    id: 2,
    number: '02',
    name: 'The Concrete Wildfire',
    subtitle: 'Systemic Displacement',
    type: 'DEFENSE',
    typeColor: 'var(--color-accent)',
    description:
      'Hold the Last Bridge against Xelthara Enforcers herding 412 refugees into containment. Allow the final group to slip into the unmapped slums and escape colonial registration.',
    objectives: ['Activate the Energy Tower Shield', 'Hold the bridge under mortar fire', 'Destroy the Siege Walkers'],
    threat: 'OMEGA',
    difficulty: 3,
    bgClass: 'level-bg--story',
  },
  {
    id: 3,
    number: '03',
    name: 'Neon Rust and Data Dust',
    subtitle: 'Life in the Margins',
    type: 'STEALTH',
    typeColor: 'var(--color-secondary)',
    description:
      'The Under-City — a network of maintenance shafts beneath a gleaming Xelthara settlement. Siphon their energy to power the marginalized population\'s life-support systems while evading Purity Patrols.',
    objectives: ['Activate Phase-Shift capability', 'Bypass Xelthara Sentinels undetected', 'Detonate EMP at the perimeter gate'],
    threat: 'HIGH',
    difficulty: 3,
    bgClass: 'level-bg--cyber',
  },
  {
    id: 4,
    number: '04',
    name: 'Shattered Aegis of the Void Commander',
    subtitle: 'Asymmetric Warfare',
    type: 'ASSAULT',
    typeColor: 'var(--color-gold)',
    description:
      'The shattered Pacific Defense Hub. Dismantle the last broadcast tower and distribute frequency chips to civilian resistance cells — finalizing the transition to guerrilla warfare.',
    objectives: ['Initiate the Network Hotwire sequence', 'Sync the micro-drone fleet', 'Fire the Orbital Strike — Obliteration Protocol'],
    threat: 'CRITICAL',
    difficulty: 4,
    bgClass: 'level-bg--story',
  },
];

function DifficultyPips({ level }) {
  return (
    <div className="difficulty-pips">
      {[1, 2, 3, 4].map(i => (
        <span
          key={i}
          className={`difficulty-pip ${i <= level ? 'difficulty-pip--active' : ''}`}
        />
      ))}
    </div>
  );
}

export default function Levels({ bgCyber, bgDojo, bgStory, hideSectionHeader }) {
  const [activeLevel, setActiveLevel] = useState(0);
  const level = LEVELS[activeLevel];

  const bgMap = { 'level-bg--cyber': bgCyber, 'level-bg--dojo': bgDojo, 'level-bg--story': bgStory };

  return (
    <section className="levels section noise-bg" id="levels">
      <div className="container">
        {!hideSectionHeader && (
          <div className="levels__header">
            <span className="section-label">// campaign levels</span>
            <h2 className="section-title">
              The <span className="text-gradient-primary">Mission</span> Awaits
            </h2>
            <div className="divider" />
            <p className="section-description">
              Five unique levels with increasing difficulty. Each mission unlocks progressively
              as you push back the Xelthara occupation.
            </p>
          </div>
        )}

        <div className="levels__layout">
          {/* Level selector */}
          <div className="levels__selector">
            {LEVELS.map((lv, i) => (
              <button
                key={lv.id}
                id={`level-select-${lv.id}`}
                className={`levels__select-item ${i === activeLevel ? 'levels__select-item--active' : ''}`}
                onClick={() => setActiveLevel(i)}
                style={i === activeLevel ? { '--lv-color': lv.typeColor } : {}}
              >
                <span className="levels__select-num">{lv.number}</span>
                <div className="levels__select-info">
                  <span className="levels__select-name">{lv.name}</span>
                  <span
                    className="levels__select-type"
                    style={i === activeLevel ? { color: lv.typeColor } : {}}
                  >
                    {lv.type}
                  </span>
                </div>
                <div className="levels__select-arrow">›</div>
              </button>
            ))}
          </div>

          {/* Level detail */}
          <div className="levels__detail" key={level.id}>
            {/* Level background image */}
            <div className="levels__detail-bg">
              <img
                src={bgMap[level.bgClass]}
                alt={level.name}
                className="levels__detail-img"
              />
              <div className="levels__detail-overlay" />
              <div className="levels__detail-header">
                <div className="levels__detail-badges">
                  <span className="levels__detail-num">LEVEL {level.number}</span>
                  <span
                    className="levels__detail-type"
                    style={{ color: level.typeColor, borderColor: `${level.typeColor}50` }}
                  >
                    {level.type}
                  </span>
                </div>
                <div className="levels__detail-threat">
                  <span className="levels__detail-threat-label">THREAT:</span>
                  <span
                    className="levels__detail-threat-value"
                    style={{
                      color: level.threat === 'MINIMAL' ? 'var(--color-green)'
                        : level.threat === 'HIGH' ? 'var(--color-gold)'
                        : 'var(--color-accent)'
                    }}
                  >
                    {level.threat}
                  </span>
                </div>
              </div>
            </div>

            {/* Level info */}
            <div className="levels__detail-body">
              <div className="levels__detail-title-row">
                <div>
                  <h3 className="levels__detail-title">{level.name}</h3>
                  <p className="levels__detail-subtitle">{level.subtitle}</p>
                </div>
                <div className="levels__detail-difficulty">
                  <span className="levels__difficulty-label">DIFFICULTY</span>
                  <DifficultyPips level={level.difficulty} />
                </div>
              </div>

              <p className="levels__detail-desc">{level.description}</p>

              <div className="levels__objectives">
                <h4 className="levels__objectives-title">MISSION OBJECTIVES</h4>
                {level.objectives.map((obj, i) => (
                  <div key={i} className="levels__objective">
                    <span
                      className="levels__objective-num"
                      style={{ color: level.typeColor, background: `${level.typeColor}15` }}
                    >
                      {String(i + 1).padStart(2, '0')}
                    </span>
                    <span className="levels__objective-text">{obj}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
