import React, { useState, useRef, useEffect } from 'react';
import './Characters.css';

const CHARACTERS = [
  {
    id: 'ninja-frog',
    name: 'Ninja Frog',
    codename: 'SHADOW-1',
    role: 'Infiltrator',
    color: '#39d353',
    glowColor: 'rgba(57, 211, 83, 0.5)',
    description:
      'A master of stealth and agility. The Ninja Frog can wall-jump and double-jump to reach impossible positions. Lightning-fast movement makes this warrior the ideal scout.',
    abilities: ['Wall Jump', 'Double Jump', 'Sprint', 'Shadow Strike'],
    stats: { speed: 95, power: 60, agility: 99, defense: 45 },
    spriteFile: 'Ninja Frog',
    lore: '"They do not see me until it is too late. In the margins between their spotlights, I exist."',
  },
  {
    id: 'mask-dude',
    name: 'Mask Dude',
    codename: 'PHANTOM-2',
    role: 'Balanced Fighter',
    color: '#00d4ff',
    glowColor: 'rgba(0, 212, 255, 0.5)',
    description:
      'The masked warrior offers the perfect balance of offense and defense. Reliable in all situations, Mask Dude is the go-to choice for strategic gameplay.',
    abilities: ['Balanced Attack', 'Shield Block', 'Ground Slam', 'Recovery Jump'],
    stats: { speed: 75, power: 75, agility: 75, defense: 75 },
    spriteFile: 'Mask Dude',
    lore: '"The mask is not a disguise. It is a declaration that I refuse to be recognized by their systems."',
  },
  {
    id: 'pink-man',
    name: 'Pink Man',
    codename: 'NOVA-3',
    role: 'Heavy Hitter',
    color: '#ff69b4',
    glowColor: 'rgba(255, 105, 180, 0.5)',
    description:
      'Do not let the color fool you. Pink Man is the powerhouse of the team — devastating attacks with high damage output and impressive defensive capabilities.',
    abilities: ['Power Strike', 'Charge Attack', 'Heavy Stomp', 'Rage Mode'],
    stats: { speed: 55, power: 99, agility: 50, defense: 85 },
    spriteFile: 'Pink Man',
    lore: '"They laugh at my color. They stop laughing very quickly."',
  },
  {
    id: 'virtual-guy',
    name: 'Virtual Guy',
    codename: 'MATRIX-4',
    role: 'Tech Specialist',
    color: '#9b4dca',
    glowColor: 'rgba(155, 77, 202, 0.5)',
    description:
      'A digital consciousness embedded in a physical chassis. Virtual Guy can interface with enemy systems and exploit technical vulnerabilities that others cannot perceive.',
    abilities: ['EMP Burst', 'Hack Interface', 'Phase Shift', 'Data Siphon'],
    stats: { speed: 80, power: 65, agility: 88, defense: 55 },
    spriteFile: 'Virtual Guy',
    lore: '"I live in the space between their firewalls. I am the ghost in their machine."',
  },
];

function StatBar({ label, value, color }) {
  return (
    <div className="stat-bar">
      <span className="stat-bar__label">{label}</span>
      <div className="stat-bar__track">
        <div
          className="stat-bar__fill"
          style={{
            width: `${value}%`,
            background: color,
            boxShadow: `0 0 8px ${color}`,
          }}
        />
      </div>
      <span className="stat-bar__value">{value}</span>
    </div>
  );
}

export default function Characters({ sprites, hideSectionHeader }) {
  const [activeChar, setActiveChar] = useState(0);
  const char = CHARACTERS[activeChar];

  return (
    <section className="characters section" id="characters">
      <div className="container">
        {!hideSectionHeader && (
          <div className="characters__header">
            <span className="section-label">// warrior selection</span>
            <h2 className="section-title">
              Choose Your <span className="text-gradient-primary">Fighter</span>
            </h2>
            <div className="divider" />
            <p className="section-description">
              Four elite warriors stand against the Xelthara occupation. Each brings
              unique abilities to the fight for Earth's freedom.
            </p>
          </div>
        )}

        {/* Selector tabs */}
        <div className="characters__tabs">
          {CHARACTERS.map((c, i) => (
            <button
              key={c.id}
              id={`char-tab-${c.id}`}
              className={`characters__tab ${i === activeChar ? 'characters__tab--active' : ''}`}
              style={i === activeChar ? { borderColor: c.color, color: c.color } : {}}
              onClick={() => setActiveChar(i)}
            >
              <span
                className="characters__tab-dot"
                style={i === activeChar ? { background: c.color, boxShadow: `0 0 8px ${c.color}` } : {}}
              />
              {c.name}
            </button>
          ))}
        </div>

        {/* Character card */}
        <div className="characters__display" key={char.id}>
          <div className="characters__sprite-panel">
            <div
              className="characters__sprite-bg"
              style={{ '--char-color': char.color, '--char-glow': char.glowColor }}
            >
              {/* Sprite animation */}
              <div className="characters__sprite-container">
                {sprites[char.spriteFile] ? (
                  <img
                    src={sprites[char.spriteFile]}
                    alt={`${char.name} idle sprite`}
                    className="characters__sprite"
                  />
                ) : (
                  <div className="characters__sprite-placeholder">{char.name[0]}</div>
                )}
              </div>
              {/* Glow rings */}
              <div className="characters__glow-ring" style={{ '--char-color': char.color }} />
              <div className="characters__glow-ring characters__glow-ring--2" style={{ '--char-color': char.color }} />
            </div>

            {/* Lore quote */}
            <div className="characters__lore" style={{ borderColor: `${char.color}40` }}>
              <span className="characters__lore-text">{char.lore}</span>
            </div>
          </div>

          <div className="characters__info">
            <div className="characters__meta">
              <span
                className="characters__codename"
                style={{ color: char.color }}
              >
                ◈ {char.codename}
              </span>
              <span className="characters__role">{char.role}</span>
            </div>

            <h3 className="characters__name" style={{ textShadow: `0 0 30px ${char.glowColor}` }}>
              {char.name}
            </h3>

            <p className="characters__description">{char.description}</p>

            <div className="characters__abilities">
              <h4 className="characters__abilities-title">ABILITIES</h4>
              <div className="characters__ability-grid">
                {char.abilities.map(ab => (
                  <div
                    key={ab}
                    className="characters__ability"
                    style={{ borderColor: `${char.color}40`, color: char.color }}
                  >
                    <span className="characters__ability-icon" style={{ background: `${char.color}20` }}>⚡</span>
                    {ab}
                  </div>
                ))}
              </div>
            </div>

            <div className="characters__stats">
              <h4 className="characters__stats-title">COMBAT STATS</h4>
              {Object.entries(char.stats).map(([key, val]) => (
                <StatBar
                  key={key}
                  label={key.toUpperCase()}
                  value={val}
                  color={char.color}
                />
              ))}
            </div>
          </div>
        </div>

        {/* Unlock note */}
        <div className="characters__unlock">
          <span className="characters__unlock-icon">🔐</span>
          <span>
            <strong>Cyber Shogun</strong> — Bonus unlockable character with sword & karate attacks.
            Complete the campaign to unlock this legendary warrior.
          </span>
        </div>
      </div>
    </section>
  );
}
